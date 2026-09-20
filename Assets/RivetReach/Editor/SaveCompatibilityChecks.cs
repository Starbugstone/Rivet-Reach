using System;
using System.IO;
using System.Linq;
using UnityEngine;
namespace RivetReach.Editor
{
    // Do not manufacture old saves from today's catalog: newer items change its
    // fingerprint. Historical envelopes below are immutable native player captures.
    public static class SaveCompatibilityChecks
    {
        public static void Run()
        {
            var items=ItemRegistry.Load();var store=new SaveStore("unused",items);
            var clone=ScriptableObject.CreateInstance<ItemRegistry>();
            var entry=new SaveEntry{Id=Guid.NewGuid().ToString("N"),WorldId=Guid.NewGuid().ToString("N"),Name="Content contract",Seed=17,UtcTicks=DateTime.UtcNow.Ticks,GeneratorVersion=TerrainGenerator.Version};
            try
            {
                using(var reader=store.Open(store.Encode(entry,w=>w.Write(314)),out var restored))
                    if(reader.ReadInt32()!=314||restored.Seed!=17)throw new Exception("Current content round trip differs");
                clone.items=items.items.Select(i=>JsonUtility.FromJson<ItemDefinition>(JsonUtility.ToJson(i))).ToArray();
                void Reject(string name)
                {
                    byte[] bytes=new SaveStore("unused",clone).Encode(entry,w=>w.Write(314));
                    try{using var reader=store.Open(bytes,out _);}
                    catch(InvalidDataException){return;}
                    throw new Exception("Accepted incompatible current content: "+name);
                }
                clone.items[0].attackDamage++;Reject("changed combat definition");clone.items[0].attackDamage--;
                clone.items=clone.items.Where(i=>i.runtimeId!=BedId.Bed).ToArray();clone.InvalidateIndex();Reject("missing current item");
                var results=new System.Collections.Generic.List<string>();
                const string directory=".docs/verification/release-review-2026-09-19/legacy";
                foreach(var fixture in ReleaseLegacyFixtures.Validate(directory,store))
                    results.Add("PASS pinned historical envelope schema "+fixture.Schema+" "+fixture.Identity.Name+" "+fixture.Path);
                FixtureRejections(directory,store,results);CoroutineCleanupChecks(results);
                Directory.CreateDirectory("Logs/ReleaseReview");File.WriteAllLines("Logs/ReleaseReview/save-compatibility.txt",results);
            }
            finally{UnityEngine.Object.DestroyImmediate(clone);}
        }
        static void FixtureRejections(string source,SaveStore store,System.Collections.Generic.List<string> results)
        {
            string temporary=Path.Combine(Path.GetTempPath(),"RivetReach-legacy-checks-"+Guid.NewGuid().ToString("N"));Directory.CreateDirectory(temporary);
            void Reject(string label)
            {
                try{ReleaseLegacyFixtures.Validate(temporary,store);}
                catch(InvalidDataException){results.Add("PASS reject "+label);return;}
                throw new Exception("Historical fixture gate accepted "+label);
            }
            try
            {
                Reject("empty historical fixture directory");
                string manifest=File.ReadAllText(Path.Combine(source,"manifest.json"));string manifestPath=Path.Combine(temporary,"manifest.json");File.WriteAllText(manifestPath,manifest);
                Reject("manifest without historical captures");
                foreach(string path in Directory.GetFiles(source,"*.rrsave"))File.Copy(path,Path.Combine(temporary,Path.GetFileName(path)));
                string last=Path.Combine(temporary,"schema-17.rrsave");File.Delete(last);Reject("partial historical capture set");File.Copy(Path.Combine(source,"schema-17.rrsave"),last);
                File.WriteAllText(manifestPath,manifest.Replace("\"schema\": 2","\"schema\": 1"));Reject("duplicated schema in manifest");File.WriteAllText(manifestPath,manifest);
                File.Copy(last,Path.Combine(temporary,"extra.rrsave"));Reject("extra unmanifested historical capture");File.Delete(Path.Combine(temporary,"extra.rrsave"));
                string first=Path.Combine(temporary,"schema-01.rrsave");byte[] original=File.ReadAllBytes(first);byte[] changed=(byte[])original.Clone();changed[changed.Length-1]^=1;File.WriteAllBytes(first,changed);
                Reject("modified historical capture bytes");
                using(var hash=System.Security.Cryptography.SHA256.Create())
                {
                    string oldHash=BitConverter.ToString(hash.ComputeHash(original)).Replace("-","").ToLowerInvariant();
                    string newHash=BitConverter.ToString(hash.ComputeHash(changed)).Replace("-","").ToLowerInvariant();
                    File.WriteAllText(manifestPath,manifest.Replace(oldHash,newHash));Reject("modified capture and forged matching manifest hash");
                }
                File.WriteAllBytes(first,original);File.WriteAllText(manifestPath,manifest);
                using(var reader=store.Open(original,out var identity))
                {File.WriteAllText(manifestPath,manifest.Replace(identity.Id,new string('0',32)));Reject("mismatched manifest source/save identity");}
                File.WriteAllText(manifestPath,manifest);
                if(ReleaseLegacyFixtures.Validate(temporary,store).Length!=17)throw new Exception("Restored historical fixture set differs");
                results.Add("PASS restored complete fixture set validates after independent corruptions");
            }
            finally{Directory.Delete(temporary,true);}
        }
        static void CoroutineCleanupChecks(System.Collections.Generic.List<string> results)
        {
            var failures=new System.Collections.Generic.List<Exception>();var order=new System.Collections.Generic.List<string>();
            bool restored=false;var original=new Exception("Nested test failure");var cleanup=new Exception("Cleanup test failure");
            System.Collections.IEnumerator Child(bool fail)
            {yield return null;if(fail)throw original;}
            System.Collections.IEnumerator Middle(bool fail,bool cleanupFail)
            {try{yield return Child(fail);}finally{order.Add("middle");if(cleanupFail)throw cleanup;}}
            System.Collections.IEnumerator Parent(bool fail,bool cleanupFail)
            {try{yield return Middle(fail,cleanupFail);}finally{restored=true;order.Add("parent");}}
            var runner=VerificationCoroutines.Run(Parent(true,true),failures.Add);while(runner.MoveNext()){}
            if(!restored||!order.SequenceEqual(new[]{"middle","parent"})||failures.Count!=2||!ReferenceEquals(failures[0],original)||!ReferenceEquals(failures[1].InnerException,cleanup))
                throw new Exception("Nested failure must preserve its original exception, report cleanup failure and restore outer state");
            results.Add("PASS nested coroutine failure restores outer state and reports original plus cleanup exceptions in order");
            restored=false;order.Clear();failures.Clear();runner=VerificationCoroutines.Run(Parent(false,false),failures.Add);
            if(!runner.MoveNext())throw new Exception("Cancellation fixture did not suspend inside a child");((IDisposable)runner).Dispose();
            if(!restored||!order.SequenceEqual(new[]{"middle","parent"})||failures.Count!=0)throw new Exception("Cancelling nested verification failed to restore parent state");
            results.Add("PASS disposing suspended nested verification runs every parent finally exactly once");
            restored=false;order.Clear();runner=VerificationCoroutines.Run(Parent(false,false),failures.Add);while(runner.MoveNext()){}
            if(!restored||!order.SequenceEqual(new[]{"middle","parent"})||failures.Count!=0)throw new Exception("Successful nested verification cleanup differs");
            results.Add("PASS successful nested verification restores state without reporting a failure");
        }
    }
}

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using UnityEngine;

namespace RivetReach
{
    // Verification only: pin the actual historical captures, not newly manufactured
    // saves or a mutable manifest's claim that its replacement bytes are historical.
    public static class ReleaseLegacyFixtures
    {
        [Serializable] sealed class Manifest { public Entry[] entries=Array.Empty<Entry>(); }
        [Serializable] sealed class Entry { public int schema=0;public string file="",source="",sha256=""; }
        public sealed class Fixture
        {
            public int Schema {get;}
            public string Path {get;}
            public byte[] Data {get;}
            public SaveEntry Identity {get;}
            internal Fixture(int schema,string path,byte[] data,SaveEntry identity){Schema=schema;Path=path;Data=data;Identity=identity;}
        }
        static readonly string[] hashes={
            "a39d8ac09e0b1df4ab2ed6c79883faea9a8b0b54b325b4c0325114256ad7be8c",
            "e245d3777639b4d7a8f6334611cf15cdd0275304fe24b267fa25145766c728a4",
            "8b2bcaac2294550e66cbf12a983e60e9762136693482e4b4eb90199011c28d8d",
            "71de76fc386f5f23dbef90ac5438ed91842e06c123928db1d1820250d751c4b1",
            "6fa8367f99754ad17852f1bef4304ddce58556e8d5d82a543d12551218cacf35",
            "d69ee8fafe9c0be71791a84046b6d9b824d609932bbe7f3b31316de4b532a17c",
            "2af0344361f5cf48d15776a38007d8aff08c086f45d064c7f47245490cd1d87e",
            "2330572ecd418336501fec473a8c51c93cb2c8507506b2e274222ac62cbdcc0d",
            "b12896dc7ee124016c6743e5dfacb8841407f3093ed2206558d387a1d42e2789",
            "cd979ac0d3ad68ddc45f2448c4f75ffd24615098ac4ca7a0714b7fa93c41a253",
            "5bd9cc9f0149964e8033ffdb294b486f21cfd523cea1f83211e3cf23a3ed8886",
            "82bd466ec309622cd96e036e45539fd7aac36111a13e3edf9a94bf255774c4b9",
            "7384b4579a57b7c1ddcf2389d61cf7134b2bdcd8740e2593992d4e0a4b11b3be",
            "887ce40176c5ecac28f0519e8fb7e0657c2a117646d3c62041d73801cc90edb0",
            "4709f1ee91207fd1fcbbb5debfe48fc728385992db5f887e081cddfd60c0b9f9",
            "a16c430ea926aa3bfd9f71ea1352cf0f82b62c32684317ca0f9a0f1a7b8fdbcb",
            "b8aac6815e557a86aada8244e72906701d8a600d97ee978896c0954b77dab3c3"
        };
        public static Fixture[] Validate(string directory,SaveStore store)
        {
            if(!Directory.Exists(directory))throw new InvalidDataException("Historical fixture directory is missing.");
            string manifestPath=System.IO.Path.Combine(directory,"manifest.json");
            if(!File.Exists(manifestPath))throw new InvalidDataException("Historical fixture manifest is missing.");
            Manifest manifest;
            try{manifest=JsonUtility.FromJson<Manifest>("{\"entries\":"+File.ReadAllText(manifestPath)+"}");}
            catch(ArgumentException e){throw new InvalidDataException("Invalid historical fixture manifest.",e);}
            if(manifest?.entries==null||manifest.entries.Length!=hashes.Length)throw new InvalidDataException("Historical fixtures must cover every schema from 1 through 17 exactly once.");
            var entries=new Entry[hashes.Length];
            foreach(var entry in manifest.entries)
            {
                if(entry==null||entry.schema<1||entry.schema>hashes.Length||entries[entry.schema-1]!=null||
                    entry.file!="schema-"+entry.schema.ToString("00")+".rrsave"||entry.sha256!=hashes[entry.schema-1]||string.IsNullOrWhiteSpace(entry.source))
                    throw new InvalidDataException("Historical fixture identity or pinned hash differs from schemas 1 through 17.");
                entries[entry.schema-1]=entry;
            }
            if(Directory.GetFiles(directory,"*.rrsave").Length!=hashes.Length)throw new InvalidDataException("Historical fixture files are missing or duplicated; expected exactly 17 captures.");
            var fixtures=new Fixture[hashes.Length];using var sha=SHA256.Create();
            for(int i=0;i<entries.Length;i++)
            {
                var entry=entries[i];string path=System.IO.Path.Combine(directory,entry.file);
                if(!File.Exists(path))throw new InvalidDataException("Missing historical fixture: "+entry.file);
                byte[] data=File.ReadAllBytes(path);
                string digest=BitConverter.ToString(sha.ComputeHash(data)).Replace("-","").ToLowerInvariant();
                if(digest!=hashes[i])throw new InvalidDataException("Historical fixture bytes changed: "+entry.file);
                using var reader=store.Open(data,out var identity);
                string sourceId=System.IO.Path.GetFileNameWithoutExtension(entry.source.Replace('\\','/'));
                if(reader.Format!=entry.schema||identity.Id!=sourceId)throw new InvalidDataException("Historical fixture envelope identity differs: "+entry.file);
                fixtures[i]=new Fixture(entry.schema,path,data,identity);
            }
            return fixtures;
        }
    }

    // Unity yield instructions still reach Unity; nested IEnumerator exceptions
    // become report failures and every suspended parent's finally is unwound.
    public static class VerificationCoroutines
    {
        public static IEnumerator Run(IEnumerator routine,Action<Exception> reportFailure)
        {
            var routines=new Stack<IEnumerator>();routines.Push(routine);
            void Dispose(IEnumerator current)
            {
                try{(current as IDisposable)?.Dispose();}
                catch(Exception error){reportFailure(new InvalidOperationException("Verification coroutine cleanup failed.",error));}
            }
            try
            {
                while(routines.Count>0)
                {
                    bool more;object current=null;
                    try{more=routines.Peek().MoveNext();if(more)current=routines.Peek().Current;}
                    catch(Exception error){reportFailure(error);break;}
                    if(!more){Dispose(routines.Pop());continue;}
                    if(current is IEnumerator nested){routines.Push(nested);continue;}
                    yield return current;
                }
            }
            finally{while(routines.Count>0)Dispose(routines.Pop());}
        }
    }
}

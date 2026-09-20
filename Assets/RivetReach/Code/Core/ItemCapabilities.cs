using System;
using System.Collections.Generic;
using System.Linq;
namespace RivetReach
{
    public interface IItemCapability { }
    public interface IEdible : IItemCapability { int FoodPoints {get;} }
    public interface IBurnable : IItemCapability { }
    public interface IBoilerFuel : IBurnable { }
    public interface IVegetable : IItemCapability { }
    public interface IFruit : IItemCapability { }
    public interface IGrain : IItemCapability { }
    public interface IMushroom : IItemCapability { }
    public interface IPreparedFood : IItemCapability { }
    public interface IMeat : IItemCapability { }
    public interface IRawMeat : IItemCapability { }
    public interface IFish : IItemCapability { }
    public interface IRawFish : IItemCapability { }
    public interface IEgg : IItemCapability { }
    public interface IRawEgg : IItemCapability { }
    public interface ISeed : IItemCapability { }
    public interface IFibre : IItemCapability { }
    public interface ICordage : IItemCapability { }
    public interface IFabric : IItemCapability { }
    public interface IFeather : IItemCapability { }
    public interface IRawOre : IItemCapability { }
    public interface IIngot : IItemCapability { }
    public interface ILogMaterial : IItemCapability { }
    public interface IPlankMaterial : IItemCapability { }
    public interface IFishingRod : IItemCapability { }
    public interface ICompostable : IItemCapability { }

    // Immutable capability components are built once with the registry. A definition
    // can expose several interfaces; stacks keep compact IDs and instance state.
    // Serialized labels are compatibility/authoring input, never tick-time type tests.
    internal sealed class ItemCapabilityIndex
    {
        sealed class Food : IEdible { public int FoodPoints {get;} public Food(int points){FoodPoints=points;} }
        sealed class Fuel : IBurnable { public static readonly Fuel Value=new Fuel(); }
        sealed class BoilerFuel : IBoilerFuel { public static readonly BoilerFuel Value=new BoilerFuel(); }
        sealed class Vegetable : IVegetable { public static readonly Vegetable Value=new Vegetable(); }
        sealed class Fruit : IFruit { public static readonly Fruit Value=new Fruit(); }
        sealed class Grain : IGrain { public static readonly Grain Value=new Grain(); }
        sealed class Mushroom : IMushroom { public static readonly Mushroom Value=new Mushroom(); }
        sealed class PreparedFood : IPreparedFood { public static readonly PreparedFood Value=new PreparedFood(); }
        sealed class Meat : IMeat { public static readonly Meat Value=new Meat(); }
        sealed class RawMeat : IRawMeat { public static readonly RawMeat Value=new RawMeat(); }
        sealed class Fish : IFish { public static readonly Fish Value=new Fish(); }
        sealed class RawFish : IRawFish { public static readonly RawFish Value=new RawFish(); }
        sealed class Egg : IEgg { public static readonly Egg Value=new Egg(); }
        sealed class RawEgg : IRawEgg { public static readonly RawEgg Value=new RawEgg(); }
        sealed class Seed : ISeed { public static readonly Seed Value=new Seed(); }
        sealed class Fibre : IFibre { public static readonly Fibre Value=new Fibre(); }
        sealed class Cordage : ICordage { public static readonly Cordage Value=new Cordage(); }
        sealed class Fabric : IFabric { public static readonly Fabric Value=new Fabric(); }
        sealed class Feather : IFeather { public static readonly Feather Value=new Feather(); }
        sealed class RawOre : IRawOre { public static readonly RawOre Value=new RawOre(); }
        sealed class Ingot : IIngot { public static readonly Ingot Value=new Ingot(); }
        sealed class LogMaterial : ILogMaterial { public static readonly LogMaterial Value=new LogMaterial(); }
        sealed class PlankMaterial : IPlankMaterial { public static readonly PlankMaterial Value=new PlankMaterial(); }
        sealed class FishingRod : IFishingRod { public static readonly FishingRod Value=new FishingRod(); }
        sealed class Compostable : ICompostable { public static readonly Compostable Value=new Compostable(); }
        readonly Dictionary<Type,IItemCapability[]> interfaces=new Dictionary<Type,IItemCapability[]>();
        readonly Dictionary<Type,ItemSelector> typedSelectors=new Dictionary<Type,ItemSelector>();
        readonly Dictionary<string,ItemSelector> selectors=new Dictionary<string,ItemSelector>(StringComparer.Ordinal);
        readonly Dictionary<string,Type> labelTypes=new Dictionary<string,Type>(StringComparer.Ordinal);
        void Add<T>(byte id,string label,T capability) where T:class,IItemCapability
        {
            if(!interfaces.TryGetValue(typeof(T),out var table)){table=new IItemCapability[256];interfaces.Add(typeof(T),table);}
            table[id]=capability;labelTypes[label]=typeof(T);
        }
        public T Get<T>(byte id) where T:class,IItemCapability
            =>interfaces.TryGetValue(typeof(T),out var table)?table[id] as T:null;
        public ItemSelector Select<T>() where T:class,IItemCapability
            =>typedSelectors.TryGetValue(typeof(T),out var selector)?selector:null;
        public ItemSelector Select(string label)
            =>label!=null&&selectors.TryGetValue(label,out var selector)?selector:null;
        public bool Matches(byte id,string label)=>Select(label)?.Matches(id)??false;
        public ItemCapabilityIndex(ItemDefinition[] items)
        {
            if(items==null)return;
            var labels=new Dictionary<string,bool[]>(StringComparer.Ordinal);
            bool HasLabel(byte id,string label)=>labels.TryGetValue(label,out var members)&&members[id];
            foreach(var item in items)
            {
                byte id=item.runtimeId;
                foreach(string label in item.tags)
                {
                    if(!labels.TryGetValue(label,out var table)){table=new bool[256];labels.Add(label,table);}table[id]=true;
                }
                if(HasLabel(id,"edible"))Add<IEdible>(id,"edible",new Food(item.foodPoints));
                if(HasLabel(id,"boiler_fuel"))
                {
                    if(!HasLabel(id,"burnable"))throw new InvalidOperationException("Boiler fuel must also be burnable: "+item.stableId);
                    Add<IBoilerFuel>(id,"boiler_fuel",BoilerFuel.Value);Add<IBurnable>(id,"burnable",BoilerFuel.Value);
                }
                else if(HasLabel(id,"burnable"))Add<IBurnable>(id,"burnable",Fuel.Value);
                if(HasLabel(id,"vegetable"))Add<IVegetable>(id,"vegetable",Vegetable.Value);
                if(HasLabel(id,"fruit"))Add<IFruit>(id,"fruit",Fruit.Value);
                if(HasLabel(id,"grain"))Add<IGrain>(id,"grain",Grain.Value);
                if(HasLabel(id,"mushroom"))Add<IMushroom>(id,"mushroom",Mushroom.Value);
                if(HasLabel(id,"prepared_food"))Add<IPreparedFood>(id,"prepared_food",PreparedFood.Value);
                if(HasLabel(id,"meat"))Add<IMeat>(id,"meat",Meat.Value);
                if(HasLabel(id,"raw_meat"))Add<IRawMeat>(id,"raw_meat",RawMeat.Value);
                if(HasLabel(id,"fish"))Add<IFish>(id,"fish",Fish.Value);
                if(HasLabel(id,"raw_fish"))Add<IRawFish>(id,"raw_fish",RawFish.Value);
                if(HasLabel(id,"egg"))Add<IEgg>(id,"egg",Egg.Value);
                if(HasLabel(id,"raw_egg"))Add<IRawEgg>(id,"raw_egg",RawEgg.Value);
                if(HasLabel(id,"seed"))Add<ISeed>(id,"seed",Seed.Value);
                if(HasLabel(id,"fibre"))Add<IFibre>(id,"fibre",Fibre.Value);
                if(HasLabel(id,"cordage"))Add<ICordage>(id,"cordage",Cordage.Value);
                if(HasLabel(id,"fabric"))Add<IFabric>(id,"fabric",Fabric.Value);
                if(HasLabel(id,"feather"))Add<IFeather>(id,"feather",Feather.Value);
                if(HasLabel(id,"raw_ore"))Add<IRawOre>(id,"raw_ore",RawOre.Value);
                if(HasLabel(id,"ingot"))Add<IIngot>(id,"ingot",Ingot.Value);
                if(HasLabel(id,"log"))Add<ILogMaterial>(id,"log",LogMaterial.Value);
                if(HasLabel(id,"planks"))Add<IPlankMaterial>(id,"planks",PlankMaterial.Value);
                if(HasLabel(id,"fishing_rod"))Add<IFishingRod>(id,"fishing_rod",FishingRod.Value);
                if(HasLabel(id,"compostable"))Add<ICompostable>(id,"compostable",Compostable.Value);
            }
            // Known labels and typed queries share one compiled selector. A recipe
            // cannot accept a member that lacks its corresponding interface.
            foreach(var pair in interfaces)
                typedSelectors.Add(pair.Key,new ItemSelector(Enumerable.Range(1,255).Where(id=>pair.Value[id]!=null).Select(id=>(byte)id)));
            foreach(var pair in labels)
                selectors.Add(pair.Key,labelTypes.TryGetValue(pair.Key,out var type)?typedSelectors[type]:
                    new ItemSelector(Enumerable.Range(1,255).Where(id=>pair.Value[id]).Select(id=>(byte)id)));
        }
    }
}

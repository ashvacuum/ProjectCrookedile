using System.Collections.Generic;

namespace Crookedile.Data.Database
{
    public interface IDatabase<T> where T : UnityEngine.ScriptableObject
    {
        T GetByID(string id);
        List<T> GetAll();
        bool Contains(string id);
        int Count { get; }
    }
}

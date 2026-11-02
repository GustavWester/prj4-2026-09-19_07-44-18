using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace Tests.PlayMode
{
    /// <summary>
    /// Hjælper til PlayMode-tests: bygger GameObjects direkte i testens tomme scene
    /// (ingen scene fra spillet loades) og rydder dem op igen bagefter.
    /// </summary>
    public class TestScene
    {
        private readonly List<Object> created = new();

        /// <summary>
        /// Laver et inaktivt GameObject, så felter kan sættes før Awake kører.
        /// Kald SetActive(true) bagefter.
        /// </summary>
        public GameObject Inactive(string name = "Test")
        {
            var go = new GameObject(name);
            go.SetActive(false);
            created.Add(go);
            return go;
        }

        public GameObject Active(string name = "Test")
        {
            var go = new GameObject(name);
            created.Add(go);
            return go;
        }

        public T Track<T>(T obj) where T : Object
        {
            created.Add(obj);
            return obj;
        }

        /// <summary>
        /// Fjerner alt med det samme (DestroyImmediate), så næste test ikke ser rester,
        /// fx en LocalPlayer.Current der først ville forsvinde i slutningen af framen.
        /// </summary>
        public void Cleanup()
        {
            foreach (var obj in created)
                if (obj != null) Object.DestroyImmediate(obj);
            created.Clear();
        }

        /// <summary>Sætter et privat [SerializeField] felt, ligesom inspectoren ville gøre.</summary>
        public static void SetField(object target, string field, object value) => Find(target, field).SetValue(target, value);

        /// <summary>Læser et privat felt, fx en NetworkVariable.</summary>
        public static T GetField<T>(object target, string field) => (T)Find(target, field).GetValue(target);

        /// <summary>Kalder en privat metode, fx en [Rpc]. Netcode sender den, præcis som ved et normalt kald.</summary>
        public static object Call(object target, string method, params object[] args)
        {
            var info = target.GetType().GetMethod(method, Flags);
            if (info == null) throw new MissingFieldException(target.GetType().Name, method);
            return info.Invoke(target, args);
        }

        private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public;

        // private felter i en base-klasse (fx PlayerSpell) ses kun fra base-typen selv
        private static FieldInfo Find(object target, string field)
        {
            for (var type = target.GetType(); type != null; type = type.BaseType)
            {
                var info = type.GetField(field, Flags);
                if (info != null) return info;
            }
            throw new MissingFieldException(target.GetType().Name, field);
        }

        private class MissingFieldException : System.Exception
        {
            public MissingFieldException(string type, string field) : base($"{type} har intet felt eller metode '{field}'") { }
        }
    }
}

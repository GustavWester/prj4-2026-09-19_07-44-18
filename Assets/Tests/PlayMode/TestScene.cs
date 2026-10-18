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
        public static void SetField(object target, string field, object value)
        {
            var info = target.GetType().GetField(field, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
            if (info == null) throw new MissingFieldException(target.GetType().Name, field);
            info.SetValue(target, value);
        }

        private class MissingFieldException : System.Exception
        {
            public MissingFieldException(string type, string field) : base($"{type} har intet felt '{field}'") { }
        }
    }
}

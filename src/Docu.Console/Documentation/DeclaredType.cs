using System;
using System.Collections.Generic;
using Docu.Documentation.Comments;
using Docu.Parsing.Model;

namespace Docu.Documentation
{
    public class DeclaredType : BaseDocumentationElement, IReferencable
    {
        private readonly List<Method> methods = new List<Method>();
        private readonly List<Property> properties = new List<Property>();
        private Type representedType;

        public DeclaredType(TypeIdentifier name, Namespace ns)
            : base(name)
        {
            Namespace = ns;
            Summary = new List<IComment>();
            Interfaces = new List<IReferencable>();
        }

        public Namespace Namespace { get; private set; }

        public IList<Method> Methods
        {
            get { return methods; }
        }

        public IList<Property> Properties
        {
            get { return properties; }
        }

        public IList<IComment> Summary { get; internal set; }
        public IReferencable ParentType { get; set; }
        public IList<IReferencable> Interfaces { get; set; }

        public string PrettyName
        {
            get { return representedType == null ? Name : representedType.GetPrettyName(); }
        }

        public string FullName
        {
            get { return (Namespace == null ? "" : Namespace.FullName + ".") + PrettyName; }
        }

        public void Resolve(IDictionary<Identifier, IReferencable> referencables)
        {
            if (referencables.ContainsKey(identifier))
            {
                IReferencable referencable = referencables[identifier];
                var type = referencable as DeclaredType;

                if (type == null)
                    throw new InvalidOperationException("Cannot resolve to '" + referencable.GetType().FullName + "'");

                Namespace = type.Namespace;
                representedType = type.representedType;
                ParentType = type.ParentType;
                Interfaces = type.Interfaces;

                if (!Namespace.IsResolved)
                    Namespace.Resolve(referencables);
                if (ParentType != null && !ParentType.IsResolved)
                    ParentType.Resolve(referencables);

                foreach (IReferencable face in Interfaces)
                {
                    if (!face.IsResolved)
                        face.Resolve(referencables);
                }

                IsResolved = true;
            }
            else
                ConvertToExternalReference();
        }

        internal void AddMethod(Method method)
        {
            methods.Add(method);
        }

        internal void AddProperty(Property property)
        {
            properties.Add(property);
        }

        public void Sort()
        {
            methods.Sort((x, y) => x.Name.CompareTo(y.Name));
            properties.Sort((x, y) => x.Name.CompareTo(y.Name));
        }

        public override string ToString()
        {
            return base.ToString() + " { Name = '" + Name + "'}";
        }

        public static DeclaredType Unresolved(TypeIdentifier typeIdentifier, Type type, Namespace ns)
        {
            var declaredType = new DeclaredType(typeIdentifier, ns) { IsResolved = false, representedType = type };

            if (type.BaseType != null)
                declaredType.ParentType = Unresolved(
                    Identifier.FromType(type.BaseType),
                    type.BaseType,
                    Namespace.Unresolved(Identifier.FromNamespace(type.BaseType.Namespace)));

            IEnumerable<Type> interfaces = GetInterfaces(type);

            foreach (Type face in interfaces)
            {
                declaredType.Interfaces.Add(
                    Unresolved(
                        Identifier.FromType(face),
                        face,
                        Namespace.Unresolved(Identifier.FromNamespace(face.Namespace))));
            }

            return declaredType;
        }

        private static IEnumerable<Type> GetInterfaces(Type type)
        {
            var interfaces = new List<Type>(type.GetInterfaces());

            foreach (Type face in type.GetInterfaces())
            {
                FilterInterfaces(interfaces, face);
            }

            if (type.BaseType != null)
                FilterInterfaces(interfaces, type.BaseType);

            return interfaces;
        }

        private static void FilterInterfaces(IList<Type> topLevelInterfaces, Type type)
        {
            foreach (Type face in type.GetInterfaces())
            {
                if (topLevelInterfaces.Contains(face))
                {
                    topLevelInterfaces.Remove(face);
                    continue;
                }

                FilterInterfaces(topLevelInterfaces, face);
            }

            if (type.BaseType != null)
                FilterInterfaces(topLevelInterfaces, type.BaseType);
        }

        public static DeclaredType Unresolved(TypeIdentifier typeIdentifier, Namespace ns)
        {
            return new DeclaredType(typeIdentifier, ns) { IsResolved = false };
        }
    }
}
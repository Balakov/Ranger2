using System;
using System.Collections.Generic;
using System.ComponentModel;

namespace Ranger2.DynamicViewModelProperties
{
    public delegate TValue GetterDelegate<T, TValue>(T viewModel, string name);
    public delegate void SetterDelegate<T, TValue>(T viewModel, string name, TValue value);

    public class DynamicPropertyManager<TTarget> : IDisposable
    {
        private readonly DynamicTypeDescriptionProvider m_provider;
        private readonly TTarget m_target;

        public DynamicPropertyManager()
        {
            Type type = typeof(TTarget);
            m_provider = new DynamicTypeDescriptionProvider(type);
            TypeDescriptor.AddProvider(m_provider, type);
        }

        public DynamicPropertyManager(TTarget target)
        {
            this.m_target = target;

            m_provider = new DynamicTypeDescriptionProvider(typeof(TTarget));
            TypeDescriptor.AddProvider(m_provider, target);
        }

        public IList<PropertyDescriptor> Properties => m_provider.Properties;

        public void Dispose()
        {
            if (ReferenceEquals(m_target, null))
            {
                TypeDescriptor.RemoveProvider(m_provider, typeof(TTarget));
            }
            else
            {
                TypeDescriptor.RemoveProvider(m_provider, m_target);
            }
        }

        public static DynamicPropertyDescriptor<TTargetType, TPropertyType> CreateProperty<TTargetType, TPropertyType>(string displayName,
                                                                                                                       GetterDelegate<TTargetType, TPropertyType> getter,
                                                                                                                       SetterDelegate<TTargetType, TPropertyType> setter,
                                                                                                                       Attribute[] attributes)
        {
            return new DynamicPropertyDescriptor<TTargetType, TPropertyType>(displayName, getter, setter, attributes);
        }

        public static DynamicPropertyDescriptor<TTargetType, TPropertyType> CreateProperty<TTargetType, TPropertyType>(string displayName,
                                                                                                                       GetterDelegate<TTargetType, TPropertyType> getHandler,
                                                                                                                       Attribute[] attributes)
        {
            return new DynamicPropertyDescriptor<TTargetType, TPropertyType>(displayName, getHandler, (t, p, s) => { }, attributes);
        }
    }

    public class DynamicTypeDescriptionProvider : TypeDescriptionProvider
    {
        private readonly TypeDescriptionProvider m_provider;
        private readonly List<PropertyDescriptor> m_properties = new List<PropertyDescriptor>();

        public DynamicTypeDescriptionProvider(Type type)
        {
            m_provider = TypeDescriptor.GetProvider(type);
        }

        public IList<PropertyDescriptor> Properties => m_properties;

        public override ICustomTypeDescriptor GetTypeDescriptor(Type objectType, object instance) => new DynamicCustomTypeDescriptor(this, m_provider.GetTypeDescriptor(objectType, instance));

        private class DynamicCustomTypeDescriptor : CustomTypeDescriptor
        {
            private readonly DynamicTypeDescriptionProvider provider;

            public DynamicCustomTypeDescriptor(DynamicTypeDescriptionProvider provider, ICustomTypeDescriptor descriptor) : base(descriptor)
            {
                this.provider = provider;
            }

            public override PropertyDescriptorCollection GetProperties() => GetProperties(null);

            public override PropertyDescriptorCollection GetProperties(Attribute[] attributes)
            {
                var properties = new PropertyDescriptorCollection(null);

                foreach (PropertyDescriptor property in base.GetProperties(attributes))
                {
                    properties.Add(property);
                }

                foreach (PropertyDescriptor property in provider.Properties)
                {
                    properties.Add(property);
                }

                return properties;
            }
        }
    }

    public class DynamicPropertyDescriptor<TTarget, TProperty> : PropertyDescriptor
    {
        private readonly GetterDelegate<TTarget, TProperty> m_getter;
        private readonly SetterDelegate<TTarget, TProperty> m_setter;
        private readonly string m_propertyName;

        public DynamicPropertyDescriptor(string propertyName,
                                            GetterDelegate<TTarget, TProperty> getter,
                                            SetterDelegate<TTarget, TProperty> setter,
                                            Attribute[] attributes) : base(propertyName, attributes ?? new Attribute[] { })
        {
            this.m_setter = setter;
            this.m_getter = getter;
            this.m_propertyName = propertyName;
        }

        public override bool Equals(object obj) => obj is DynamicPropertyDescriptor<TTarget, TProperty> o && o.m_propertyName.Equals(m_propertyName);
        public override int GetHashCode() => m_propertyName.GetHashCode();
        public override bool CanResetValue(object component) => true;
        public override Type ComponentType => typeof(TTarget);
        public override object GetValue(object component) => m_getter((TTarget)component, m_propertyName);
        public override bool IsReadOnly => m_setter == null;
        public override Type PropertyType => typeof(TProperty);
        public override void ResetValue(object component) { }
        public override void SetValue(object component, object value) => m_setter((TTarget)component, m_propertyName, (TProperty)value);
        public override bool ShouldSerializeValue(object component) => true;
    }
}
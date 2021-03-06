﻿using Prism.Events;
using Prism.Ioc;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Xamarin.Forms;
using Xamarin.Forms.Internals;

namespace Bit.View
{
    [ContentProperty(nameof(Actions))]
    public class AdaptiveBehavior : Behavior<VisualElement>
    {
        private double? _MaxWidth;
        public double? MaxWidth
        {
            get => _MaxWidth;
            set
            {
                _MaxWidth = value;
                OnPropertyChanged(nameof(MaxWidth));
            }
        }

        private double? _MaxHeight;
        public double? MaxHeight
        {
            get => _MaxHeight;
            set
            {
                _MaxHeight = value;
                OnPropertyChanged(nameof(MaxHeight));
            }
        }

        private double? _MinWidth;
        public double? MinWidth
        {
            get => _MinWidth;
            set
            {
                _MinWidth = value;
                OnPropertyChanged(nameof(MinWidth));
            }
        }

        private double? _MinHeight;
        public double? MinHeight
        {
            get => _MinHeight;
            set
            {
                _MinHeight = value;
                OnPropertyChanged(nameof(MinHeight));
            }
        }

        private DeviceOrientation? _Orientation;
        public DeviceOrientation? Orientation
        {
            get => _Orientation;
            set
            {
                _Orientation = value;
                OnPropertyChanged(nameof(Orientation));
            }
        }

        private List<SetPropertyAction> _Actions = new List<SetPropertyAction>();
        public List<SetPropertyAction> Actions // With SetPropertyAction type for now.
        {
            get => _Actions;
            set
            {
                _Actions = value;
                OnPropertyChanged(nameof(Actions));
            }
        }

        private bool isValid = false;

        internal async void ExecuteActionsIfRequired(VisualElement vsElement, double newScreenWidth, double newScreenHeight, DeviceOrientation newOrientation)
        {
            if (Actions == null)
                throw new InvalidOperationException($"{nameof(Actions)} may not be null");

            bool wasValid = isValid;

            isValid = (Orientation == null || Orientation == newOrientation) &&
                (MaxWidth == null || newScreenWidth <= MaxWidth) &&
                (MinWidth == null || newScreenWidth >= MinWidth) &&
                (MaxHeight == null || newScreenHeight <= MaxHeight) &&
                (MinHeight == null || newScreenHeight >= MinHeight);

            if (isValid && !wasValid)
            {
                foreach (var action in Actions)
                {
                    await Task.Yield();
                    action.InvokeAction(vsElement);
                }
            }
        }

        internal void Invalidate()
        {
            isValid = false;
        }

        protected override void OnAttachedTo(VisualElement bindable)
        {
            base.OnAttachedTo(bindable);

            Page mainPage = Application.Current?.MainPage;

            if (mainPage != null)
            {
                double width = mainPage.Width;
                double height = mainPage.Height;

                ExecuteActionsIfRequired(bindable, width, height, (width < height) ? DeviceOrientation.Portrait : DeviceOrientation.Landscape);
            }
        }
    }

    public class Screen
    {
        public static readonly BindableProperty OrientationAndScreenSizeAwareProperty = BindableProperty.CreateAttached("OrientationAndScreenSizeAware", typeof(bool?), typeof(VisualElement), null, propertyChanged: (bindable, oldValue, newValue) =>
        {
            if (newValue is bool newValueBool && newValueBool == true && bindable is VisualElement vsElement)
            {
                vsElement.Behaviors.Add(new OrientationAndScreenSizeAwareBehavior { });
            }
        });

        public static bool? GetOrientationAndScreenSizeAware(BindableObject view)
        {
            return (bool?)view.GetValue(OrientationAndScreenSizeAwareProperty);
        }

        public static void SetOrientationAndScreenSizeAware(BindableObject view, bool? value)
        {
            view.SetValue(OrientationAndScreenSizeAwareProperty, value);
        }
    }

    public class InvalidateAllAdaptiveBehaviors : PubSubEvent<InvalidateAllAdaptiveBehaviors>
    {

    }

    public class OrientationAndOrSizeChanged : PubSubEvent<OrientationAndOrSizeChanged>
    {
        public DeviceOrientation NewOrientation { get; set; }

        public double NewWidth { get; set; }

        public double NewHeight { get; set; }
    }

    public class OrientationAndScreenSizeAwareBehavior : Behavior<VisualElement>
    {
        OrientationAndScreenSizeAwareBehaviorImplementation implementation;

        protected override void OnAttachedTo(VisualElement bindable)
        {
            base.OnAttachedTo(bindable);

            implementation ??= new OrientationAndScreenSizeAwareBehaviorImplementation()
            {
                ElementRef = new WeakReference<VisualElement>(bindable)
            };

            IEventAggregator eventAggregator = ((BitApplication)Application.Current).Container.Resolve<IEventAggregator>();

            implementation.Tokens.Add(eventAggregator.GetEvent<OrientationAndOrSizeChanged>()
                .SubscribeAsync(implementation.OnOrientationChanged, keepSubscriberReferenceAlive: true, threadOption: ThreadOption.UIThread));

            implementation.Tokens.Add(eventAggregator.GetEvent<InvalidateAllAdaptiveBehaviors>()
                .SubscribeAsync(implementation.OnInvalidateAllAdaptiveBehaviors, keepSubscriberReferenceAlive: true, threadOption: ThreadOption.UIThread));
        }
    }

    internal class OrientationAndScreenSizeAwareBehaviorImplementation
    {
        public WeakReference<VisualElement> ElementRef { get; set; }
        public List<SubscriptionToken> Tokens { get; set; } = new List<SubscriptionToken>();

        public Task OnOrientationChanged(OrientationAndOrSizeChanged newOrientationEvent)
        {
            if (ElementRef.TryGetTarget(out VisualElement vsElement) && vsElement.BindingContext != null)
            {
                foreach (AdaptiveBehavior adaptiveBehavior in vsElement.Behaviors.OfType<AdaptiveBehavior>())
                {
                    adaptiveBehavior.ExecuteActionsIfRequired(vsElement, newOrientationEvent.NewWidth, newOrientationEvent.NewHeight, newOrientationEvent.NewOrientation);
                }
            }
            else // Element is detached from visual tree. It might be garbage collected too.
            {
                Tokens.ForEach(t => t.Dispose());
            }

            return Task.CompletedTask;
        }

        public Task OnInvalidateAllAdaptiveBehaviors(InvalidateAllAdaptiveBehaviors @event)
        {
            if (ElementRef.TryGetTarget(out VisualElement vsElement) && vsElement.BindingContext != null)
            {
                foreach (AdaptiveBehavior adaptiveBehavior in vsElement.Behaviors.OfType<AdaptiveBehavior>())
                {
                    adaptiveBehavior.Invalidate();
                }
            }
            else // Element is detached from visual tree. It might be garbage collected too.
            {
                Tokens.ForEach(t => t.Dispose());
            }

            return Task.CompletedTask;
        }
    }

    public class SetPropertyAction : TriggerAction<VisualElement>
    {
        public virtual void InvokeAction(object sender)
        {
            Invoke(sender);
        }

        public VisualElement TargetElement { get; set; }

        public string Property { get; set; }

        public object Value { get; set; }

        public object Delay { get; set; }

        static readonly Dictionary<Type, ConvertFromInvariantString> xamlTypeConvertersCache = new Dictionary<Type, ConvertFromInvariantString>();

        delegate object ConvertFromInvariantString(string value);

        protected override async void Invoke(VisualElement sender)
        {
            if (Property == null)
                throw new InvalidOperationException($"{nameof(Property)} may not be null");

            if (Value == null)
                throw new InvalidOperationException($"{nameof(Value)} may not be null");

            if (TargetElement == null)
                TargetElement = sender;

            Type targetElementType = TargetElement.GetType();

            PropertyInfo propertyInfo = targetElementType.GetProperty(Property);

            if (propertyInfo == null)
                throw new ArgumentException($"Could not find property {Property} in {targetElementType}");

            Type propertyType = propertyInfo.PropertyType;

            if (propertyType != Value.GetType())
            {
                if (ConvertUsingXamlTypeConverter(propertyInfo, Value, out object resultValue))
                {
                    Value = resultValue;
                }
                else
                {
                    Value = ConvertUsingDotNetTypeConverter(propertyType, Value);
                }
            }

            if (Delay != null)
            {
                int delay = Convert.ToInt32(Delay);
                await Task.Delay(delay);
            }

            propertyInfo.SetValue(TargetElement, Value);
        }

        static object ConvertUsingDotNetTypeConverter(Type destinationType, object input)
        {
            destinationType = Nullable.GetUnderlyingType(destinationType) ?? destinationType;

            if (destinationType.IsEnum)
            {
                return Enum.Parse(destinationType, (string)input);
            }
            else if (destinationType == typeof(string))
            {
                return Convert.ToString(input, CultureInfo.InvariantCulture);
            }
            else if (destinationType == typeof(bool))
            {
                return Convert.ToBoolean(input, CultureInfo.InvariantCulture);
            }
            else if (destinationType == typeof(byte))
            {
                return Convert.ToByte(input, CultureInfo.InvariantCulture);
            }
            else if (destinationType == typeof(short))
            {
                return Convert.ToInt16(input, CultureInfo.InvariantCulture);
            }
            else if (destinationType == typeof(int))
            {
                return Convert.ToInt32(input, CultureInfo.InvariantCulture);
            }
            else if (destinationType == typeof(long))
            {
                return Convert.ToInt64(input, CultureInfo.InvariantCulture);
            }
            else if (destinationType == typeof(double))
            {
                return Convert.ToDouble(input, CultureInfo.InvariantCulture);
            }
            else if (destinationType == typeof(float))
            {
                return Convert.ToSingle(input, CultureInfo.InvariantCulture);
            }
            else if (destinationType == typeof(decimal))
            {
                return Convert.ToDecimal(input, CultureInfo.InvariantCulture);
            }

            throw new InvalidCastException($"Value {input} is not convertible to {destinationType}");
        }

        static bool ConvertUsingXamlTypeConverter(PropertyInfo propertyInfo, object inputValue, out object resultValue)
        {
            if (propertyInfo.GetCustomAttribute(typeof(TypeConverterAttribute)) is TypeConverterAttribute propertyAttr)
            {
                resultValue = UseXamlTypeConverter(Type.GetType(propertyAttr.ConverterTypeName), inputValue);
                return true;
            }
            else if (propertyInfo.PropertyType.GetCustomAttribute(typeof(TypeConverterAttribute)) is TypeConverterAttribute classAttr)
            {
                resultValue = UseXamlTypeConverter(Type.GetType(classAttr.ConverterTypeName), inputValue);
                return true;
            }

            resultValue = null;

            return false;
        }

        static object UseXamlTypeConverter(Type converterType, object value)
        {
            if (!xamlTypeConvertersCache.ContainsKey(converterType))
            {
                object converter = Activator.CreateInstance(converterType);

                MethodInfo methodInfo = converterType.GetMethod(nameof(ConvertFromInvariantString));

                ConvertFromInvariantString converterDelegate = ((ConvertFromInvariantString)methodInfo
                    .CreateDelegate(typeof(ConvertFromInvariantString), converter));

                xamlTypeConvertersCache.Add(converterType, converterDelegate);
            }

            return xamlTypeConvertersCache[converterType].Invoke((string)value);
        }
    }
}

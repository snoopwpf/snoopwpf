using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Reflection.Emit;


namespace Snoop {
    public class ReflectionHelper {
        #region inner classes
        struct HelperKey {
            public bool Equals(HelperKey other) {
                return type == other.type && string.Equals(handlerName, other.handlerName) && Equals(handlerType, other.handlerType) && castParameters.Equals(other.castParameters) && forcedResultType == other.forcedResultType && forcedThisArgType == other.forcedThisArgType && parametersCount == other.parametersCount;
            }
            public override bool Equals(object obj) {
                if (ReferenceEquals(null, obj)) return false;
                return obj is HelperKey && Equals((HelperKey)obj);
            }
            public override int GetHashCode() {
                return getHashCode;
            }
            int GetHashCodeInternal() {
                unchecked {
                    int hashCode = (type != null ? type.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (handlerName != null ? handlerName.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (handlerType != null ? handlerType.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ castParameters.GetHashCode();
                    hashCode = (hashCode * 397) ^ (forcedResultType != null ? forcedResultType.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ (forcedThisArgType != null ? forcedThisArgType.GetHashCode() : 0);
                    hashCode = (hashCode * 397) ^ parametersCount.GetHashCode();
                    return hashCode;
                }

            }
            public HelperKey(Type type, string handlerName, Type handlerType, bool castParameters = false, Type forcesResultType = null, Type forcedThisArgType = null, int? parametersCount = null) {
                this.type = type;
                this.handlerName = handlerName;
                this.handlerType = handlerType;
                this.castParameters = castParameters;
                forcedResultType = forcesResultType;
                this.forcedThisArgType = forcedThisArgType;
                this.parametersCount = parametersCount;
                this.getHashCode = 0;
                this.getHashCode = GetHashCodeInternal();
            }

            public static bool operator ==(HelperKey left, HelperKey right) {
                return left.Equals(right);
            }
            public static bool operator !=(HelperKey left, HelperKey right) {
                return !left.Equals(right);
            }
            readonly Type type;
            readonly string handlerName;
            readonly Type handlerType;
            readonly bool castParameters;
            readonly Type forcedResultType;
            readonly Type forcedThisArgType;
            readonly int? parametersCount;
            readonly int getHashCode;
        }
        #endregion
        Dictionary<HelperKey, object> InvokeInfo { get; set; }
        Dictionary<HelperKey, Type> PropertyTypeInfo { get; set; }
        public bool HasContent { get { return InvokeInfo.Count > 0; } }

        public ReflectionHelper() {
            InvokeInfo = new Dictionary<HelperKey, object>();
            PropertyTypeInfo = new Dictionary<HelperKey, Type>();
        }
        Func<object, object> CreateGetter(PropertyInfo info) {
            return (Func<object, object>)CreateMethodHandlerImpl(info.GetGetMethod(true), null, typeof(Func<object, object>), true, typeof(object), typeof(object));
        }
        Action<object, object> CreateSetter(PropertyInfo info) {
            if (!info.CanWrite)
                throw new NotSupportedException("no setter");
            return (Action<object, object>)CreateMethodHandlerImpl(info.GetSetMethod(true), null, typeof(Action<object, object>), true, null, typeof(object));
        }
        static object CreateMethodHandlerImpl(object instance, string methodName, BindingFlags bindingFlags, Type instanceType, Type delegateType, bool castParameters, Type forcedResultType = null, Type forcedThisArgType = null, int? parametersCount = null, Type[] typeParameters = null) {
            MethodInfo mi = null;
            if (instance != null)
                mi = GetMethod(instance.GetType(), methodName, bindingFlags, parametersCount, typeParameters);
            mi = mi ?? GetMethod(instanceType, methodName, bindingFlags, parametersCount, typeParameters);
            return CreateMethodHandlerImpl(mi, instanceType, delegateType, castParameters, forcedResultType, forcedThisArgType);
        }
        static MethodInfo GetMethod(Type type, string methodName, BindingFlags bindingFlags, int? parametersCount = null, Type[] typeParameters = null) {
            if (parametersCount != null) {
                return type.GetMethods(bindingFlags).Where(x => x.Name == methodName).First(x => x.GetParameters().Count() == parametersCount.Value);
            }
            if (typeParameters != null) {
                return type.GetMethods(bindingFlags).Where(x => x.Name == methodName).First(x => {
                    int i = 0;
                    foreach (var param in x.GetParameters()) {
                        if (!typeParameters[i].IsAssignableFrom(param.ParameterType))
                            return false;
                        i++;
                    }
                    return true;
                });
            }
            return type.GetMethod(methodName, bindingFlags);
        }
        static void CastClass(ILGenerator generator, Type sourceType, Type targetType) {
            if (Equals(null, targetType))
                return;
            if (Equals(sourceType, targetType))
                return;
            bool oneIsVoid = Equals(typeof(void), sourceType) || Equals(typeof(void), targetType);
            bool sourceIsNull = Equals(null, sourceType);
            if (oneIsVoid && !sourceIsNull)
                throw new InvalidOperationException(string.Format("Cast from {0} to {1} is not supported", sourceType, targetType));            
            if (Equals(null, sourceType)) {
                if (targetType.IsClass)
                    generator.Emit(OpCodes.Castclass, targetType);
                else
                    generator.Emit(OpCodes.Unbox_Any, targetType);
            }
            //box
            if (sourceType.IsValueType && !targetType.IsValueType) {
                generator.Emit(OpCodes.Box, sourceType);
                generator.Emit(OpCodes.Castclass, targetType);
            }
            //unbox
            if (!sourceType.IsValueType && targetType.IsValueType)
                generator.Emit(OpCodes.Unbox_Any, targetType);
            //cast
            if (Equals(sourceType.IsValueType, targetType.IsValueType) && !(sourceType == targetType))
                generator.Emit(OpCodes.Castclass, targetType);
        }               

        static object CreateMethodHandlerImpl(MethodInfo mi, Type instanceType, Type delegateType, bool castParameters, Type forcedResultType = null, Type forcedThisArgType = null) {
            bool isStatic = mi.IsStatic;            

            if (isStatic && forcedThisArgType != null) 
                throw new ArgumentException("Do not specify the forcedThisArgType argument for static methods");
            if (Equals(typeof(void), mi.ReturnType) && forcedResultType != null && forcedResultType != typeof(void))
                throw new ArgumentException("The forcedResultType argument should be either null or typeof(void) for the method which doesn't return a value");

            var thisArgType = forcedThisArgType ?? instanceType ?? mi.DeclaringType;
            var returnType = forcedResultType ?? mi.ReturnType;

            Type[] delegateGenericArguments;
            bool skipArgumentLengthCheck = false;
            var sourceParametersTypes = mi.GetParameters().Select(x => x.ParameterType).ToArray();
            if (delegateType == null) {
                delegateType = MakeGenericDelegate(sourceParametersTypes, returnType, isStatic ? null : thisArgType);
                delegateGenericArguments = sourceParametersTypes;
                skipArgumentLengthCheck = true;
            } else {
                delegateGenericArguments = delegateType.GetMethod("Invoke").GetParameters().Select(x => x.ParameterType).ToArray();
            }

            if (!skipArgumentLengthCheck && delegateGenericArguments.Length != (isStatic ? sourceParametersTypes.Count() : sourceParametersTypes.Count() + 1))
                throw new ArgumentException("Invalid delegate arguments count");

            var resultParametersTypes = delegateGenericArguments.Skip(isStatic ? 0 : 1);
            var dynamicMethodParameterTypes = (isStatic ? resultParametersTypes : new Type[] { thisArgType }.Concat(resultParametersTypes)).ToArray();

            DynamicMethod dm = new DynamicMethod(
            string.Empty,
            returnType,
            dynamicMethodParameterTypes,
            true);

            var ig = dm.GetILGenerator();
                        
            if (!isStatic) {
                var isValueType = mi.DeclaringType.IsValueType;
                if (isValueType) {
                    ig.DeclareLocal(mi.DeclaringType);
                }
                ig.Emit(OpCodes.Ldarg_0);                
                CastClass(ig, thisArgType, mi.DeclaringType);

                if (isValueType) {
                    ig.Emit(OpCodes.Stloc_0);
                    ig.Emit(OpCodes.Ldloca_S, 0);
                }                
            }

            short argumentIndex = mi.IsStatic ? (short)0 : (short)1;
            for (int parameterIndex = 0; parameterIndex < sourceParametersTypes.Length; parameterIndex++) {
                ig.Emit(OpCodes.Ldarg, argumentIndex++);
                CastClass(ig, resultParametersTypes.ElementAt(parameterIndex), sourceParametersTypes[parameterIndex]);
            }

            ig.Emit(mi.IsVirtual ? OpCodes.Callvirt : OpCodes.Call, mi);
            
            CastClass(ig, mi.ReturnType, forcedResultType);
            ig.Emit(OpCodes.Ret);
            return dm.CreateDelegate(delegateType);
        }
        static Type MakeGenericDelegate(Type[] parameterTypes, Type returnType, Type thisArgType) {
            Type resultType = null;
            bool hasReturnType = returnType != null && returnType != typeof(void);
            var parametersCount = parameterTypes.Length;
            if (thisArgType != null)
                parametersCount += 1;
            switch (parametersCount) {
                case 0:
                    resultType = hasReturnType ? typeof(Func<>) : typeof(Action);
                    break;
                case 1:
                    resultType = hasReturnType ? typeof(Func<,>) : typeof(Action<>);
                    break;
                case 2:
                    resultType = hasReturnType ? typeof(Func<,,>) : typeof(Action<,>);
                    break;
                default:
                    resultType = hasReturnType ? typeof(Func<>).Assembly.GetType(string.Format("System.Func`{0}", parametersCount + 1)) : typeof(Func<>).Assembly.GetType(string.Format("System.Action`{0}", parametersCount));
                    break;
            }
            var lst = new List<Type>();
            if (thisArgType != null)
                lst.Add(thisArgType);
            lst.AddRange(parameterTypes);
            if (hasReturnType)
                lst.Add(returnType);
            if (lst.Count == 0)
                return resultType;
            return resultType.MakeGenericType(lst.ToArray());
        }
        static Delegate CreateFieldGetterOrSetter<TElement, TField>(bool isGetter, Type delegateType, Type declaringType, string fieldName, BindingFlags bFlags) {
            FieldInfo fieldInfo = declaringType.GetField(fieldName, bFlags);
            bool isStatic = fieldInfo.IsStatic;
            DynamicMethod dm;
            if(isGetter)
                dm = new DynamicMethod(string.Empty, typeof(object), new Type[] { typeof(object) }, true);
            else
                dm = new DynamicMethod(string.Empty, typeof(void), new Type[] { typeof(object), typeof(object) }, true);
            var ig = dm.GetILGenerator();

            short argIndex = 0;
            if (!isStatic) {
                ig.Emit(OpCodes.Ldarg, argIndex++);
                CastClass(ig, typeof(TElement), fieldInfo.DeclaringType);
            }
            if (!isGetter) {
                ig.Emit(OpCodes.Ldarg, argIndex++);
                CastClass(ig, typeof(TField), fieldInfo.FieldType);
                ig.Emit(isStatic ? OpCodes.Stsfld : OpCodes.Stfld, fieldInfo);
            } else {
                ig.Emit(isStatic ? OpCodes.Ldsfld : OpCodes.Ldfld, fieldInfo);
                CastClass(ig, fieldInfo.FieldType, typeof(TField));
            }            
            ig.Emit(OpCodes.Ret);
            return dm.CreateDelegate(delegateType);
        }
        public static Func<TElement, TField> CreateFieldGetter<TElement,TField>(Type declaringType, string fieldName, BindingFlags bFlags = BindingFlags.Instance | BindingFlags.Public) {
            return (Func<TElement, TField>)CreateFieldGetterOrSetter<TElement, TField>(true, typeof(Func<TElement, TField>), declaringType, fieldName, bFlags);
        }
        public static Action<TElement, TField> CreateFieldSetter<TElement, TField>(Type declaringType, string fieldName, BindingFlags bFlags = BindingFlags.Instance | BindingFlags.Public) {
            return (Action<TElement, TField>)CreateFieldGetterOrSetter<TElement, TField>(false, typeof(Action<TElement, TField>), declaringType, fieldName, bFlags);
        }
        public static Delegate CreateInstanceMethodHandler(object instance, string methodName, BindingFlags bindingFlags, Type instanceType, bool castParameters = false, Type forcedResultType = null, Type forcedThisArgType = null, int? parametersCount = null, Type[] typeParameters = null) {
            return (Delegate)CreateMethodHandlerImpl(instance, methodName, bindingFlags, instanceType, null, castParameters, forcedResultType, forcedThisArgType, parametersCount, typeParameters);
        }
        public static TDelegate CreateInstanceMethodHandler<TInstance, TDelegate>(TInstance entity, string methodName, BindingFlags bindingFlags, bool castParameters = false, Type forcedResultType = null, Type forcedThisArgType = null, int? parametersCount = null, Type[] typeParameters = null) where TInstance : class {
            return (TDelegate)CreateMethodHandlerImpl(entity, methodName, bindingFlags, typeof(TInstance), typeof(TDelegate), castParameters, forcedResultType, forcedThisArgType, parametersCount, typeParameters);
        }
        public static TDelegate CreateInstanceMethodHandler<TDelegate>(object instance, string methodName, BindingFlags bindingFlags, Type instanceType, bool castParameters = false, Type forcedResultType = null, Type forcedThisArgType = null, int? parametersCount = null, Type[] typeParameters = null) {
            return (TDelegate)CreateMethodHandlerImpl(instance, methodName, bindingFlags, instanceType, typeof(TDelegate), castParameters, forcedResultType, forcedThisArgType, parametersCount, typeParameters);
        }
        public T GetStaticMethodHandler<T>(Type entityType, string methodName, BindingFlags bindingFlags, bool castParameters = false, Type forcedResultType = null, Type forcedThisArgType = null) where T : class {
            object method;
            var key = new HelperKey(entityType, methodName, typeof(T));
            if (!InvokeInfo.TryGetValue(key, out method)) {
                method = CreateMethodHandlerImpl(null, methodName, bindingFlags, entityType, typeof(T), castParameters, forcedResultType, forcedThisArgType);
                InvokeInfo[key] = method;
            }
            return (T)method;
        }
        public T GetInstanceMethodHandler<TInstance, T>(TInstance entity, string methodName, BindingFlags bindingFlags, bool castParameters = false, Type forcedResultType = null, Type forcedThisArgType = null, int? parametersCount = null) where TInstance : class {
            object method;
            var key = new HelperKey(entity.GetType(), methodName, typeof(T), castParameters, forcedResultType, forcedThisArgType, parametersCount);
            if (!InvokeInfo.TryGetValue(key, out method)) {
                method = CreateInstanceMethodHandler<TInstance, T>(entity, methodName, bindingFlags, castParameters, forcedResultType, forcedThisArgType, parametersCount);
                InvokeInfo[key] = method;
            }
            return (T)method;
        }
        public T GetPropertyValue<T>(object entity, string propertyName, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance) {
            return (T)GetPropertyValue(entity, propertyName, bindingFlags);
        }
        public object GetPropertyValue(object entity, string propertyName, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance) {
            object getter;
            var type = entity.GetType();
            var key = new HelperKey(type, propertyName, typeof(Func<object, object>));
            if (!InvokeInfo.TryGetValue(key, out getter)) {
                var pi = type.GetProperty(propertyName, bindingFlags);
                getter = CreateGetter(pi);
                InvokeInfo[key] = getter;
            }
            var func = (Func<object, object>)getter;
            return func(entity);
        }
        public void SetPropertyValue(object entity, string propertyName, object value, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance) {
            object setter;
            var type = entity.GetType();
            var key = new HelperKey(type, propertyName, typeof(Action<object, object>));
            if (!InvokeInfo.TryGetValue(key, out setter)) {
                var pi = type.GetProperty(propertyName, bindingFlags);
                setter = CreateSetter(pi);
                InvokeInfo[key] = setter;
            }
            var del = (Action<object, object>)setter;
            del(entity, value);
        }
        public Type GetPropertyType(object entity, string propertyName, BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance) {
            Type propertyType;
            Type type = entity.GetType();
            var key = new HelperKey(type, propertyName, null);
            if (!PropertyTypeInfo.TryGetValue(key, out propertyType)) {
                var pi = type.GetProperty(propertyName, bindingFlags);
                propertyType = pi.PropertyType;
            }
            return propertyType;
        }
    }
}

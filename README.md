# Summary

A library for recording and replaying code execution flow in any environments.

This library is based on [DynamicProxy](http://www.castleproject.org/projects/dynamicproxy/) feature provided by the [Windsor.Castle](http://www.castleproject.org/projects/windsor/) library, and is utilising the [IInterceptor](https://github.com/castleproject/Core/blob/master/src/Castle.Core/DynamicProxy/IInterceptor.cs) to hijack the execution of code flow, extracting or replacing the parameters and results according to configuration.

It supports recording/replaying the code execution via predefined Http Headers, via local disk files or via in-memory context, with either Json or Bson content serialisation. As well, the provided `IProvideContext` and `ISeriliseObject` interfaces allow any customized implementation to be plugged in, which can be handy for data privacy and runtime security handling.

# Usage

The library can be easily configured using the following syntax:

```C#
InterceptorConfiguration.ConfigureFor<HttpContextProvider, JsonObjectSerializer>(container);
```

where `container` is the instance of your created Windsor.Castle IoC container.

If only a particular list of types should be recorded/replayed, then the following syntax can be used:

```C#
InterceptorConfiguration.ConfigureFor<HttpContextProvider, JsonObjectSerializer>(container, typeof(IDoSomething), ...);
```

where `IDoSomething` and its following `...` (params) are the array of the anticipated types.

# Examples

TBA

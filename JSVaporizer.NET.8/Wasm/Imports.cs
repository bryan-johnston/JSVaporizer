﻿using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;

namespace JSVaporizer;

// ============================================ //
//       Imports from JS that C# can use        //
// ============================================ //

// See: https://learn.microsoft.com/en-us/aspnet/core/client-side/dotnet-interop/?view=aspnetcore-9.0

public static partial class JSVapor
{
    [SupportedOSPlatform("browser")]
    internal partial class WasmElement
    {
        // Properties

        [JSImport("getPropertyNamesArray", "element")]
        [return: JSMarshalAs<JSType.Array<JSType.String>>]
        internal static partial string[] GetPropertyNamesArray(JSObject elem);

        [JSImport("invokeFunctionProperty", "element")]
        internal static partial void InvokeFunctionProperty(JSObject elem, string funcPropName, [JSMarshalAs<JSType.Array<JSType.Any>>] object[] args);

        // Events

        [JSImport("addEventListener", "element")]
        internal static partial int AddEventListener(JSObject elem, string type, int id);

        [JSImport("removeEventListener", "element")]
        internal static partial int RemoveEventListener(JSObject elem, string type, int id);

        [JSImport("appendChild", "element")]
        // Only finds elements connected to the DOM.
        internal static partial JSObject AppendChild(JSObject elem, JSObject childElem);

        // Attributes
        // WARNING: JS converts attrName to lower case.

        [JSImport("hasAttribute", "element")]
        internal static partial bool HasAttribute(JSObject elem, string attrName);

        [JSImport("getAttribute", "element")]
        internal static partial string? GetAttribute(JSObject elem, string attrName);

        [JSImport("setAttribute", "element")]
        internal static partial void SetAttribute(JSObject elem, string attrName, string attrValue);

        // HTMLOptionsCollection convenience

        [JSImport("getMultiSelectOptionValues", "element")]
        internal static partial string[] GetMultiSelectOptionValues(JSObject elem);
    }

    [SupportedOSPlatform("browser")]
    internal static partial class WasmDocument
    {
        [JSImport("createJSVaporizerElement", "document")]
        internal static partial JSObject? CreateJSVaporizerElement(string id, string tagName);

        [JSImport("getElementById", "document")]
        // Only finds elements connceted to the DOM.
        internal static partial JSObject? GetElementById(string id);

        [JSImport("removeElementById", "document")]
        internal static partial void RemoveElementById(string id);

        [JSImport("getElementsArrayByTagName", "document")]
        [return: JSMarshalAs<JSType.Array<JSType.Object>>]
        // Only finds elements connceted to the DOM.
        internal static partial JSObject[] GetElementsArrayByTagName(string tagName);

        [JSImport("sameJSObject", "document")]
        internal static partial bool SameJSObject(JSObject a, JSObject b);
    }

    [SupportedOSPlatform("browser")]
    internal static partial class WasmWindow
    {
        [JSImport("alert", "window")]
        internal static partial string Alert(string text);

        internal static partial class Location
        {
            [JSImport("location.href", "window")]
            internal static partial string Href();
        }

        internal static partial class Console
        {
            [JSImport("console.log", "window")]
            internal static partial string Log(string str);

            [JSImport("console.dir", "window")]
            internal static partial string Dir([JSMarshalAs<JSType.Any>] object obj);
        }
    }

    [SupportedOSPlatform("browser")]
    internal static partial class WasmJSFunctionPool
    {
        [JSImport("callJSFunction", "jsFunctionPool")]
        [return: JSMarshalAs<JSType.Any>]
        internal static partial object? CallJSFunction(string funcKey, [JSMarshalAs<JSType.Array<JSType.Any>>] object[] args);
    }

    [SupportedOSPlatform("browser")]
    internal static partial class WasmJSVGenericFuncPool
    {
        [JSImport("registerJSVGenericFunction", "jsvGenericFunctionPool")]
        public static partial bool RegisterJSVGenericFunction(string funcKey);

        [JSImport("unregisterJSVGenericFunction", "jsvGenericFunctionPool")]
        public static partial bool UnregisterJSVGenericFunction(string funcKey);
    }
}



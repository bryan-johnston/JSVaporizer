using System.Collections.Generic;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text.Json;

namespace JSVaporizer;

// ============================================ //
//          Exports that JS can use             //
// ============================================ //

// See: https://learn.microsoft.com/en-us/aspnet/core/client-side/dotnet-interop/?view=aspnetcore-9.0

public static partial class JSVapor
{
    [SupportedOSPlatform("browser")]
    internal partial class WasmExports
    {
        // ======================================================================== //
        //                  Event Listener and Generic Function pools               //
        // ======================================================================== //

        [JSExport]
        internal static int CallJSVEventListener(int id, JSObject elem, string eventType, JSObject evnt)
        {
            int behaviorMode = WasmJSVEventListenerPool.CallJSVEventListener(new EventListenerId(id), elem, eventType, evnt);
            return behaviorMode;
        }

        [JSExport]
        [return: JSMarshalAs<JSType.Any>]
        internal static object? CallJSVGenericFunction(string funcKey, [JSMarshalAs<JSType.Array<JSType.Any>>] object[] args)
        {
             return WasmJSVGenericFuncPool.CallJSVGenericFunction(funcKey, args);
        }

        // ======================================================================== //
        //                          DOM Mutation Observer                           //
        // ======================================================================== //

        // Anything marked [JSExport] is surfaced as a global JS function.
        // Parameters must be marshallable types (string, JSObject, primitives …).
        [JSExport]
        internal static void ReceiveDomMutationEvents(string mutationEventsJson)
        {
            // Deserialize a tiny DTO rather than raw DOM objects
            List<DomMutationEvent>? mutationEvents = JsonSerializer.Deserialize(mutationEventsJson, DomMutationEventContext.Default.ListDomMutationEvent);

#if DEBUG
            Window.Alert(mutationEventsJson);
#endif
            // Pass back to DOM facade
            if (mutationEvents != null)
            {
                Document.ReceiveDomMutationEvents(mutationEvents);
            }
        }
    }
}

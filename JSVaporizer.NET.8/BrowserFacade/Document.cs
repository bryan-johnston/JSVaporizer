using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices.JavaScript;
using System.Runtime.Versioning;
using System.Text.RegularExpressions;

namespace JSVaporizer;

// https://developer.mozilla.org/en-US/docs/Web/API/Document

public static partial class JSVapor
{
    public delegate bool ElementDisposalCallback(Element elem);

    [SupportedOSPlatform("browser")]
    public static class Document
    {
        // This is used for bookkeeping.
        //
        // The dictionary key is then element Id.
        //
        // In order to wrap up actual DOM elements inside our Element facade classes,
        //  we need to keep track of which ones are lying around,
        //  since you can't find them in the actual DOM when they are orphans (not connected).
        //
        // This needs to be updated upon every creation and destruction of an Element.
        // In addition, we can use the custom attribute
        //      _createdByJSVaporizerAttributeName
        // to perform reconciliation.
        private static readonly ConcurrentDictionary<string, Element> _jsvElements = new();

        // Props

        // ---------------------------------------------------------------------- //
        // ----- JSVaporizer ---------------------------------------------------- //
        // ---------------------------------------------------------------------- //

        // Methods

        // ------------------------------------------------------------------------ //
        //          Standard                                                        //
        // ------------------------------------------------------------------------ //

        public static Element CreateElement(string id, string tagName)
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                throw new ArgumentException("id cannot be empty", nameof(id));
            }
                
            if (!Regex.IsMatch(tagName, @"^[A-Za-z]\w*$"))
            {
                throw new ArgumentException($"Invalid tag name '{tagName}'", nameof(tagName));
            }
                
            // Step 1: reserve placeholder (fast).
            if (!_jsvElements.TryAdd(id, null!))
            {
                throw new JSVException($"Duplicate JSV element id '{id}'.");
            }  

            JSObject? jsObject = null;

            // Step 2️: create node outside lock
            jsObject = WasmDocument.CreateJSVaporizerElement(id, tagName);

            if (jsObject == null) // id was already present in the DOM, but not created by JSV (since _jsvElements.TryAdd() succeeded).
            {
                _jsvElements.TryRemove(id, out _);
                throw new JSVException($"DOM already contains '{id}'.");
            }

            // Step 3️: upgrade placeholder (thread-safe)
            try
            {
                // To clean up _jsvElements when Element is disposed.
                ElementDisposalCallback onDisposeCallback = elem => {
                    return _jsvElements.TryRemove(elem.Id, out _);
                };

                var elem = new Element(id, onDisposeCallback, jsObject);
                _jsvElements[id] = elem;
                return elem;
            }
            catch
            {
                _jsvElements.TryRemove(id, out _);
                jsObject.Dispose();
                WasmDocument.RemoveElementById(id); // DOM cleanup

                throw;
            }
        }


        // You cannot see external DOM elements
        // which were not created by JSVaporizer's Document.CreateElement().
        // Attempting to "adopt" elements created from external code is probably a really bad idea.
        public static Element? GetElementById(string id)
        {
            // First see if it's one that JSVaporizer made.
            if (_jsvElements.TryGetValue(id, out var elem))
            {
                return elem;
            }

            // Otherwise return whatever the DOM says we have.
            // Used existing copy if there is one,
            // so that different callers see the same exact Element object.
            // We need this behavior unless we rewrite things like event listeners,
            // which are (until changed so they are tracked inside Document!) tracked inside the individual Element objects.

            JSObject? jsObject = WasmDocument.GetElementById(id);
            if (jsObject != null)
            {
                // To clean up _jsvElements when Element is disposed.
                ElementDisposalCallback onDisposeCallback = elem => {
                    return _jsvElements.TryRemove(elem.Id, out _);
                };

                var placeholder = new Element(id, onDisposeCallback, jsObject);
                if (!_jsvElements.TryAdd(id, placeholder))
                {
                    placeholder.Dispose();
                }

                return _jsvElements[id];
            }
            else
            {
                return null;
            }
        }

        public static List<JSObject> GetElementsByTagName(string tagName)
        {
            // https://learn.microsoft.com/en-us/dotnet/api/system.runtime.interopservices.javascript.jsobject?view=net-9.0
            //      Don't carry around a JSObject for each element.
            //      They are expensive, so we will look them up on the fly
            //      then use JSObject.Dispose() to dispose them.

            JSObject[] jSObjectArr = WasmDocument.GetElementsArrayByTagName(tagName);
            return jSObjectArr.ToList();
        }

        //public static Element? QuerySelector(string selectors)
        //{
        //    throw new NotImplementedException();
        //}

        //publicstatic  List<Element> QuerySelectorAll(string selectors)
        //{
        //    throw new NotImplementedException();
        //}

        //public static void ReplaceChildren(List<Element> newChildren)
        //{
        //    throw new NotImplementedException();
        //}

        // ---------------------------------------------------------------------- //
        // ----- JSVaporizer ---------------------------------------------------- //
        // ---------------------------------------------------------------------- //

        public static Element AssertGetElementById(string id)
        {
            Element? elem = GetElementById(id);
            if (elem == null)
            {
                throw new JSVException($"Element with id={id} not found.");
            }
            return elem;
        }

        public static bool SameJSObject(JSObject a, JSObject b)
        {
            return WasmDocument.SameJSObject(a, b);
        }

        public static void ReceiveDomMutationEvents(List<DomMutationEvent> mutationEvents)
        {
            foreach (DomMutationEvent mutEv in mutationEvents)
            {
                Console.Log("");
                Console.Log("#######################################");
                Console.Log("Type:              " + mutEv.Type);
                Console.Log("AttributeName:     " + mutEv.AttributeName + " ( from: " + mutEv.AttributeOldVal + " to: " + mutEv.AttributeNewVal + " )");
                Console.Log("TargetId:          " + mutEv.TargetId);
                Console.Log("Added:             " + string.Join(", ", mutEv.Added ?? []));
                Console.Log("Removed:           " + string.Join(", ", mutEv.Removed ?? []));
            }
        }
    }
}

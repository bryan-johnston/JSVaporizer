"use strict";

let jsvExports;

export function getElement(exports) {
    jsvExports = exports;
    return {

        // Properties
        getPropertyNamesArray: (elem) => getPropertyNamesArray(elem),
        invokeFunctionProperty: (elem, funcPropName, args) => invokeFunctionProperty(elem, funcPropName, args),

        // Events
        addEventListener: (elem, eventType, funcKey) => addEventListener(elem, eventType, funcKey),
        removeEventListener: (elem, eventType, funcKey) => removeEventListener(elem, eventType, funcKey),
        appendChild: (elem, childElem) => elem.appendChild(childElem),

        // Attributes
        hasAttribute: (elem, attrName) => elem.hasAttribute(attrName),
        getAttribute: (elem, attrName) => elem.getAttribute(attrName),
        setAttribute: (elem, attrName, attrValue) => elem.setAttribute(attrName, attrValue),

        // HTMLOptionsCollection convenience
        getMultiSelectOptionValues: (elem) => getMultiSelectOptionValues(elem),

    };
}

function getPropertyNamesArray(elem) {
    var props = [];
    for (var key in elem) {
        props.push(key);
    }
    return props;
}

// id -> listener fn
const eventListenerFuncSpace = new Map();               // Map<int, Function>

// element -> Set<ids>
const eventListenerElementIds = new Map();          // Map<elementId, Set<int>>

// GC-aware purge
const gcRegistry = new FinalizationRegistry(elemId => {
    const eventListenerSet = eventListenerElementIds.get(elemId);

    if (!eventListenerSet) return;

    eventListenerSet.forEach(id => eventListenerFuncSpace.delete(id));
    eventListenerElementIds.delete(elemId);
});

function addEventListener(elem, eventType, listenerId) {
    if (!elem.id)
        throw new Error("Element id is required before adding/removing event listeners.");
    if (eventListenerFuncSpace.has(listenerId))
        throw new Error(`Listener id ${listenerId} is already registered.`);

    const eventListener = function (event) {
        let behaviorMode = jsvExports.CallJSVEventListener(listenerId, elem, eventType, event);

        // behaviorMode = 0 : preventDefault = false, stopPropagation = false
        // behaviorMode = 1 : preventDefault = false, stopPropagation = true
        // behaviorMode = 2 : preventDefault = true, stopPropagation = false
        // behaviorMode = 3 : preventDefault = true, stopPropagation = true

        const preventDefault = behaviorMode == 2 || behaviorMode == 3;
        const stopPropagation = behaviorMode == 1 || behaviorMode == 3;
        if (preventDefault) {
            event.preventDefault();
        }
        if (stopPropagation) {
            event.stopPropagation();
        }
    };
    eventListenerFuncSpace.set(listenerId, eventListener);
    elem.addEventListener(eventType, eventListener);

    if (!eventListenerElementIds.has(elem.id)) {
        eventListenerElementIds.set(elem.id, new Set());
    }
    eventListenerElementIds.get(elem.id).add(listenerId);

    gcRegistry.register(elem, elem.id);  // auto-cleanup when elem GC’d

    return eventListenerFuncSpace.size;
}

function removeEventListener(elem, eventType, listenerId) {
    if (!elem.id)
        throw new Error("Element id is required before adding/removing event listeners.");

    const listener = eventListenerFuncSpace.get(listenerId);
    if (listener) {
        elem.removeEventListener(eventType, listener);
        eventListenerFuncSpace.delete(listenerId);

        const set = eventListenerElementIds.get(elem.id);
        if (set) {
            set.delete(listenerId);
            if (set.size === 0) {
                eventListenerElementIds.delete(elem.id);
            }
        }
    }

    return eventListenerFuncSpace.size;
}

function invokeFunctionProperty(elem, funcPropName, argsArray) {
    elem[funcPropName](...argsArray);
}

function getMultiSelectOptionValues(selectElem) {
    if (!selectElem || selectElem.nodeName !== "SELECT" || !selectElem.multiple) {
        throw new Error("Expected a <select multiple> element.");
    }

    return Array.from(selectElem.selectedOptions)          // live collection
        .filter(o => !o.disabled
            && !(o.parentNode && o.parentNode.disabled && o.parentNode.nodeName.toUpperCase() === "OPTGROUP"))
        .map(o => o.value);
}

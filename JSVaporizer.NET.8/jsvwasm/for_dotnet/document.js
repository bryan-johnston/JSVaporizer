"use strict";

let jsvExports;

export function getDocument(exports) {
    jsvExports = exports;

    CreateDomMutationObserver();

    return {
        createJSVaporizerElement: (id, tagName, createdByJSVaporizerAttributeName) => createJSVaporizerElement(id, tagName, createdByJSVaporizerAttributeName),
        getElementById: (id) => document.getElementById(id),
        removeElementById: (id) => removeElementById(id),
        getElementsArrayByTagName: (tagName) => getElementsArrayByTagName(tagName)
    };
}

function removeElementById(id) {
    const n = document.getElementById(id);
    if (n) n.remove();
}

function createJSVaporizerElement(id, tagName) {
    // sentinel: already exists
    if (document.getElementById(id)) {
        return null;
    }
    let elem = document.createElement(tagName);
    elem.setAttribute("id", id);

    return elem;
};

function getElementsArrayByTagName(tagName) {
    // Note:
    //      See:
    //          https://developer.mozilla.org/en-US/docs/Web/API/Document/getElementsByTagName
    //          https://developer.mozilla.org/en-US/docs/Web/API/HTMLCollection
    //      The issue is that
    //          document.getElementsByTagName() returns an HTMLCollection.
    //      But dotnet-interop (System.Runtime.InteropServices.JavaScript) isn't cool with that type.
    //      The solution appears to be converting HTMLCollection into an array before returning to C#.

    let htmlCollection = document.getElementsByTagName(tagName);
    let elemsFound = [];

    for (let ii = 0; ii < htmlCollection.length; ii++) {
        elemsFound.push(htmlCollection.item(ii));
    }

    return elemsFound;
}

// ======================================================================== //
//                          Compare JSObjects                               //
// ======================================================================== //

export function sameJSObject(x, y) { return x === y; }

// ======================================================================== //
//                          DOM Mutation Observer                           //
// ======================================================================== //

function CreateDomMutationObserver() {
    const receiveDomMutationEvents = jsvExports.ReceiveDomMutationEvents;

    // Convert MutationRecord to plain JSON
    const serializeMutations = r => ({
        Type: r.type,
        AttributeName: r.attributeName ?? undefined,
        AttributeOldVal: r.oldValue,
        AttributeNewVal: r.target?.getAttribute(r.attributeName),
        TargetId: r.target?.id,
        Added: [...r.addedNodes].map(node => node.id),
        Removed: [...r.removedNodes].map(node => node.id)
    });

    // Observer
    const obs = new MutationObserver(mutationsList => {
        // Observer batches, but stringify to keep the bridge cheap
        let mutationsJson = JSON.stringify(mutationsList.map(serializeMutations));
        receiveDomMutationEvents(mutationsJson);
    });

    obs.observe(document.body, {
        subtree: true,
        childList: true,
        attributes: true,
        attributeOldValue: true,
        attributeFilter: ["id", "class"]
    });
}



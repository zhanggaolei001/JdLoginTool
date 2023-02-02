function createXPathFromElement(elm) {
    var allNodes = document.getElementsByTagName('*');
    for (var segs = []; elm && elm.nodeType == 1; elm = elm.parentNode) {
        if (elm.hasAttribute('id')) {
            var uniqueIdCount = 0;
            for (var n = 0; n < allNodes.length; n++) {
                if (allNodes[n].hasAttribute('id') && allNodes[n].id == elm.id) uniqueIdCount++;
                if (uniqueIdCount > 1) break;
            };
            if (uniqueIdCount == 1) {
                segs.unshift('id("' + elm.getAttribute('id') + '")');
                return segs.join('/');
            } else {
                segs.unshift(elm.localName.toLowerCase() + '[@id="' + elm.getAttribute('id') + '"]');
            }
        } else if (elm.hasAttribute('class')) {
            segs.unshift(elm.localName.toLowerCase() + '[@class="' + elm.getAttribute('class') + '"]');
        } else {
            for (i = 1, sib = elm.previousSibling; sib; sib = sib.previousSibling) {
                if (sib.localName == elm.localName) i++;
            };
            segs.unshift(elm.localName.toLowerCase() + '[' + i + ']');
        };
    };
    return segs.length ? '/' + segs.join('/') : null;
};

function lookupElementByXPath(path) {
    var evaluator = new XPathEvaluator();
    var result = evaluator.evaluate(path, document.documentElement, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null);
    return result.singleNodeValue;
}
var btn;

function getElement(e) {
    var path = createXPathFromElement(document.elementFromPoint(e.clientX, e.clientY));
    btn = getElementByXpath(path);
    var element = document.createElement('textarea');
    element.value = path;
    document.body.appendChild(element);
    element.select();
    document.execCommand('copy');
    document.body.removeChild(element); 
    if (confirm("复制元素xpath完成,是否结束选取?\r\n" + path)) {
        document.removeEventListener('mouseup', getElement); 
    }
    gui.setJsBox(`var btn=document.evaluate(\'${path}\', document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue;btn.click();`);
    return path;
   
} 
function getElementByXpath(path) {
    return document.evaluate(path, document, null, XPathResult.FIRST_ORDERED_NODE_TYPE, null).singleNodeValue;
}


 
document.addEventListener('mouseup', getElement, { passive: true })



 
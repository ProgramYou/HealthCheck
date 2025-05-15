document.getElementById("copyButton").addEventListener("click", function () {
    var res = copyToClipboard(document.getElementById("copyTarget"));

    if (res == true)
        $("#copyMessage").html("<span class=\"ns-check\">&#x2713;</span>");
    else
        $("#copyMessage").text("Copy failed. please select and copy manually. ");
});

$('#epBalanced, #epIndividual').on("change", function (e) {

    var showLoadBalanced = e.target.value == "load-balanced";

    $("#copyTarget").val(showLoadBalanced ? loadBalancedUrl : individualUrl);
    $("#copyMessage").html("");
})

$('#endpoint').on("click", function () {
    var endpointUrl = document.getElementById("copyTarget").value;
    window.open(endpointUrl, "ns-check", "");
});

$('#refresh').on("click", function () {
    location.reload();
});

function copyToClipboard(elem) {
    // create hidden text element, if it doesn't already exist
    var targetId = "_hiddenCopyText_";
    var isInput = elem.tagName === "INPUT" || elem.tagName === "TEXTAREA";
    var origSelectionStart, origSelectionEnd;
    if (isInput) {
        // can just use the original source element for the selection and copy
        target = elem;
        origSelectionStart = elem.selectionStart;
        origSelectionEnd = elem.selectionEnd;
    } else {
        // must use a temporary form element for the selection and copy
        target = document.getElementById(targetId);
        if (!target) {
            var target = document.createElement("textarea");
            target.style.position = "absolute";
            target.style.left = "-9999px";
            target.style.top = "0";
            target.id = targetId;
            document.body.appendChild(target);
        }
        target.textContent = elem.textContent;
    }
    // select the content
    var currentFocus = document.activeElement;
    target.focus();
    target.setSelectionRange(0, target.value.length);

    // copy the selection
    var succeed;
    try {
        succeed = document.execCommand("copy");
    } catch (e) {
        succeed = false;
    }
    // restore original focus
    if (currentFocus && typeof currentFocus.focus === "function") {
        currentFocus.focus();
    }

    if (isInput) {
        // restore prior selection
        elem.setSelectionRange(origSelectionStart, origSelectionEnd);
    } else {
        // clear temporary content
        target.textContent = "";
    }
    return succeed;
}

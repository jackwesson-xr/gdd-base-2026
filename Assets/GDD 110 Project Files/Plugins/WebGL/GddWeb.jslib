// GDD 110 browser bridge for WebGL builds.
// Browsers only allow a new tab from inside a real key or click event, and Unity hands input to C# a frame later,
// so the open is armed here and runs on the next key-up or pointer-up.
mergeInto(LibraryManager.library, {
  GddOpenLibrary: function (urlPtr, newTab) {
    var url = UTF8ToString(urlPtr);
    var sameTab = function () {
      try { window.top.location.href = url; } catch (e) { window.location.href = url; }
    };
    if (!newTab) { sameTab(); return; }
    var fired = false;
    var open = function () {
      if (fired) return;
      fired = true;
      document.removeEventListener('keyup', open, true);
      document.removeEventListener('pointerup', open, true);
      var w = window.open(url, '_blank');
      if (!w) { sameTab(); return; }          // popup blocked: go there in this tab instead
      try { w.focus(); } catch (e) {}
      try { window.close(); } catch (e) {}    // only works when the library opened this tab by script
    };
    document.addEventListener('keyup', open, true);
    document.addEventListener('pointerup', open, true);
  }
});

<script>
(function autoFitHero(){
  var run = function(){
    var content = document.querySelector('.hero-content');
    if(!content) return;
    var text = content.querySelector('.hero-text');
    var title = content.querySelector('.hero-title');
    var paragraphs = content.querySelector('.hero-paragraphs');
    if(!text || !title) return;
    var titleFS = parseFloat(getComputedStyle(title).fontSize);
    var textNodes = paragraphs ? paragraphs.querySelectorAll('p, li > span:last-child, h3') : [];
    var initialSizes = [];
    textNodes.forEach(function(n){ initialSizes.push(parseFloat(getComputedStyle(n).fontSize)); });
    var attempts = 0;
    var maxAttempts = 30;
    while(content.scrollHeight > content.clientHeight && attempts < maxAttempts){
      titleFS = titleFS * 0.97;
      title.style.fontSize = titleFS + 'px';
      textNodes.forEach(function(n, i){
        initialSizes[i] = initialSizes[i] * 0.96;
        n.style.fontSize = initialSizes[i] + 'px';
      });
      if(paragraphs){
        var currentGap = parseFloat(getComputedStyle(paragraphs).gap || 0);
        if(currentGap > 6) paragraphs.style.gap = (currentGap * 0.95) + 'px';
      }
      attempts++;
    }
    document.body.setAttribute('data-autofit', 'done-' + attempts);
  };
  if(document.fonts && document.fonts.ready){
    document.fonts.ready.then(function(){
      requestAnimationFrame(function(){ requestAnimationFrame(run); });
    });
  } else {
    setTimeout(run, 400);
  }
})();

(function fitPosterContent(){
  // The grid composition stacks a headline, four icon plates and a wrapping meta row inside a
  // fixed canvas. On the smaller presets a long headline or a fifth chip pushes the plates past
  // the bottom edge, so the plate block is scaled down until the whole column fits.
  var run = function(){
    var plates = document.querySelector('.grid-plates');
    if(!plates) return;
    var inner = plates.firstElementChild;
    var poster = document.querySelector('.poster');
    if(!inner || !poster) return;
    var scale = 1;
    var guard = 0;
    while(poster.scrollHeight > poster.clientHeight && scale > 0.35 && guard < 40){
      scale = scale * 0.95;
      inner.style.transform = 'scale(' + scale + ')';
      inner.style.transformOrigin = 'center';
      guard++;
    }
    document.body.setAttribute('data-gridfit', scale.toFixed(3));
  };
  if(document.fonts && document.fonts.ready){
    document.fonts.ready.then(function(){
      requestAnimationFrame(function(){ requestAnimationFrame(run); });
    });
  } else {
    setTimeout(run, 400);
  }
})();
</script>

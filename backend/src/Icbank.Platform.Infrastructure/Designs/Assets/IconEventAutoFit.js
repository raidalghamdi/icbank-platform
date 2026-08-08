<script>
// Every composition is fitted by this one pass. Fitting used to be written per layout, and the
// three compositions that never got one (split, typography, stats-hero) printed their copy straight
// through the top and bottom edges of the canvas whenever the source text ran long.
// The helpers are declared at file scope rather than inside a module closure so that each stays
// individually small enough to read; the page is a single-purpose generated document, so there is
// nothing else on it for these names to collide with.
var FIT_MIN_SCALE = 0.55;
var FIT_MAX_PASSES = 40;
var FIT_TOLERANCE = 1.5;

function fitEscapes(box, rect){
  if(rect.width === 0 && rect.height === 0) return false;
  return rect.top < box.top - FIT_TOLERANCE
    || rect.bottom > box.bottom + FIT_TOLERANCE
    || rect.left < box.left - FIT_TOLERANCE
    || rect.right > box.right + FIT_TOLERANCE;
}

function fitOverflows(poster){
  var box = poster.getBoundingClientRect();
  var nodes = poster.querySelectorAll('*');
  for(var i = 0; i < nodes.length; i++){
    var node = nodes[i];
    var clipped = node.scrollHeight - node.clientHeight > FIT_TOLERANCE;
    if(clipped && getComputedStyle(node).overflow !== 'visible') return true;
    if(fitEscapes(box, node.getBoundingClientRect())) return true;
  }
  return false;
}

function fitCollect(poster){
  var items = [];
  var nodes = poster.querySelectorAll('h1, h3, p, li, span, div');
  for(var i = 0; i < nodes.length; i++){
    var style = getComputedStyle(nodes[i]);
    items.push({
      node: nodes[i],
      font: parseFloat(style.fontSize) || 0,
      gap: parseFloat(style.gap) || 0,
      padTop: parseFloat(style.paddingTop) || 0,
      padBottom: parseFloat(style.paddingBottom) || 0,
      marginBottom: parseFloat(style.marginBottom) || 0
    });
  }
  return items;
}

function fitResize(item, scale, tight){
  if(item.font > 0) item.node.style.fontSize = (item.font * scale) + 'px';
  if(item.gap > 0) item.node.style.gap = (item.gap * tight) + 'px';
  if(item.marginBottom > 0) item.node.style.marginBottom = (item.marginBottom * tight) + 'px';
  if(item.padTop > 0) item.node.style.paddingTop = (item.padTop * tight) + 'px';
  if(item.padBottom > 0) item.node.style.paddingBottom = (item.padBottom * tight) + 'px';
}

function fitApply(items, scale){
  // Whitespace is reduced faster than type: shrinking the gaps first keeps the copy legible for
  // longer before the letterforms themselves have to give way.
  var tight = Math.max(0, 1 - ((1 - scale) * 1.8));
  for(var i = 0; i < items.length; i++) fitResize(items[i], scale, tight);
}

function fitRun(){
  var poster = document.querySelector('.poster');
  if(!poster) return;
  var items = fitCollect(poster);
  var scale = 1;
  var passes = 0;
  while(fitOverflows(poster) && scale > FIT_MIN_SCALE && passes < FIT_MAX_PASSES){
    scale = scale * 0.97;
    fitApply(items, scale);
    passes++;
  }
  document.body.setAttribute('data-fit-scale', scale.toFixed(3));
  document.body.setAttribute('data-fit-overflow', fitOverflows(poster) ? 'yes' : 'no');
}

(function bootstrapPosterFit(){
if(document.fonts && document.fonts.ready){
  document.fonts.ready.then(function(){
    requestAnimationFrame(function(){ requestAnimationFrame(fitRun); });
  });
} else {
  setTimeout(fitRun, 400);
}
})();
</script>

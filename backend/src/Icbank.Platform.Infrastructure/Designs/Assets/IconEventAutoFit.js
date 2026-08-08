<script>
// Every composition is fitted by this one pass, in both directions. Type sizes are derived from a
// fixed 2000px reference canvas, so on a 4K poster the copy occupied a third of the height and the
// rest of the frame was empty; on a dense poster it ran off the edge. Searching for the largest
// scale that still fits makes the composition fill whatever canvas it was asked for.
// The helpers are declared at file scope so each stays small enough to read; the page is a
// single-purpose generated document, so there is nothing for these names to collide with.
var FIT_TOLERANCE = 1.5;
var FIT_MIN_SCALE = 0.55;
var FIT_MAX_SCALE = 2.4;
var FIT_STEPS = 14;

function fitEscapes(box, rect){
  if(rect.width === 0 && rect.height === 0) return false;
  return rect.top < box.top - FIT_TOLERANCE
    || rect.bottom > box.bottom + FIT_TOLERANCE
    || rect.left < box.left - FIT_TOLERANCE
    || rect.right > box.right + FIT_TOLERANCE;
}

// Growing the copy can push it out of its own column long before it reaches the edge of the
// poster, so each element is also tested against the nearest declared frame.
function fitFrameOf(node){
  var frame = node.parentElement;
  while(frame && !frame.classList.contains('fit-frame')) frame = frame.parentElement;
  return frame;
}

// A frame that centres its children lets them overflow evenly in both directions, and the frame's
// own rectangle includes its padding — so copy could grow into the margin the designer reserved
// for it and still report that nothing had escaped. The content box is the real boundary.
function fitContentBox(frame){
  var rect = frame.getBoundingClientRect();
  var style = getComputedStyle(frame);
  return {
    top: rect.top + (parseFloat(style.paddingTop) || 0),
    bottom: rect.bottom - (parseFloat(style.paddingBottom) || 0),
    left: rect.left + (parseFloat(style.paddingLeft) || 0),
    right: rect.right - (parseFloat(style.paddingRight) || 0)
  };
}

function fitOverflows(poster){
  var box = poster.getBoundingClientRect();
  // The poster clips its own children, so a composition can run past the bottom edge without any
  // single descendant reporting an escape. Its own scroll height is the honest measure.
  if(poster.scrollHeight - poster.clientHeight > FIT_TOLERANCE) return true;
  var nodes = poster.querySelectorAll('*');
  for(var i = 0; i < nodes.length; i++){
    var node = nodes[i];
    var clipped = node.scrollHeight - node.clientHeight > FIT_TOLERANCE;
    if(clipped && getComputedStyle(node).overflow !== 'visible') return true;
    var rect = node.getBoundingClientRect();
    if(fitEscapes(box, rect)) return true;
    var frame = fitFrameOf(node);
    if(frame && fitEscapes(fitContentBox(frame), rect)) return true;
  }
  return false;
}

// Icon art carries its size in width and height attributes, so it is the one thing on the poster
// that a type-only pass leaves behind: the glyphs stayed at their reference size while the copy
// around them grew, and on a dense canvas they were the reason nothing else could fit.
function fitArtOf(node){
  if(node.tagName.toLowerCase() !== 'svg') return null;
  var w = parseFloat(node.getAttribute('width'));
  var h = parseFloat(node.getAttribute('height'));
  return (w > 0 && h > 0) ? { w: w, h: h } : null;
}

function fitCollect(poster){
  var items = [];
  var nodes = poster.querySelectorAll('h1, h3, p, li, span, div, svg');
  for(var i = 0; i < nodes.length; i++){
    var style = getComputedStyle(nodes[i]);
    items.push({
      node: nodes[i],
      font: parseFloat(style.fontSize) || 0,
      gap: parseFloat(style.gap) || 0,
      padTop: parseFloat(style.paddingTop) || 0,
      padBottom: parseFloat(style.paddingBottom) || 0,
      marginBottom: parseFloat(style.marginBottom) || 0,
      art: fitArtOf(nodes[i])
    });
  }
  return items;
}

function fitResize(item, scale, spacing){
  if(item.art){
    item.node.setAttribute('width', item.art.w * scale);
    item.node.setAttribute('height', item.art.h * scale);
  }
  if(item.font > 0) item.node.style.fontSize = (item.font * scale) + 'px';
  if(item.gap > 0) item.node.style.gap = (item.gap * spacing) + 'px';
  if(item.marginBottom > 0) item.node.style.marginBottom = (item.marginBottom * spacing) + 'px';
  if(item.padTop > 0) item.node.style.paddingTop = (item.padTop * spacing) + 'px';
  if(item.padBottom > 0) item.node.style.paddingBottom = (item.padBottom * spacing) + 'px';
}

function fitApply(items, scale){
  // Whitespace moves less than type in both directions: shrinking the gaps first keeps dense copy
  // legible for longer, and holding them back while growing stops a short message from turning
  // into a few enormous words separated by canyons.
  var spacing = scale >= 1 ? 1 + ((scale - 1) * 0.5) : Math.max(0, 1 - ((1 - scale) * 1.8));
  for(var i = 0; i < items.length; i++) fitResize(items[i], scale, spacing);
}

function fitSearch(poster, items, low, high){
  var best = low;
  for(var step = 0; step < FIT_STEPS; step++){
    var mid = (low + high) / 2;
    fitApply(items, mid);
    if(fitOverflows(poster)){
      high = mid;
    } else {
      best = mid;
      low = mid;
    }
  }
  fitApply(items, best);
  return best;
}

function fitRun(){
  var poster = document.querySelector('.poster');
  if(!poster) return;
  var items = fitCollect(poster);
  // Absolutely positioned compositions place their blocks by measurement rather than by flow, so
  // enlarging their type would slide one block underneath another without ever crossing an edge.
  var ceiling = poster.getAttribute('data-fit-mode') === 'shrink' ? 1 : FIT_MAX_SCALE;
  var scale = fitOverflows(poster) ? fitSearch(poster, items, FIT_MIN_SCALE, 1) : fitSearch(poster, items, 1, ceiling);
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

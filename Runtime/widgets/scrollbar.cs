using System;
using Unity.UIWidgets.animation;
using Unity.UIWidgets.foundation;
using Unity.UIWidgets.painting;
using Unity.UIWidgets.ui;

namespace Unity.UIWidgets.widgets {
    public class ScrollbarPainter : ChangeNotifier, ICustomPainter {
        public ScrollbarPainter(
            Color color,
            TextDirection textDirection,
            double thickness,
            Animation<double> fadeoutOpacityAnimation,
            double mainAxisMargin = 0.0,
            double crossAxisMargin = 0.0,
            Radius radius = null,
            double minLength = _kMinThumbExtent,
            double minOverscrollLength = _kMinThumbExtent
        ) {
            this.color = color;
            this.textDirection = textDirection;
            this.thickness = thickness;
            this.fadeoutOpacityAnimation = fadeoutOpacityAnimation;
            this.mainAxisMargin = mainAxisMargin;
            this.crossAxisMargin = crossAxisMargin;
            this.radius = radius;
            this.minLength = minLength;
            this.minOverscrollLength = minOverscrollLength;
            fadeoutOpacityAnimation.addListener(this.notifyListeners);
        }
        
        const double _kMinThumbExtent = 18.0;

        public Color color;
        public TextDirection? textDirection;
        public double thickness;
        public Animation<double> fadeoutOpacityAnimation;
        public double mainAxisMargin;
        public double crossAxisMargin;
        public Radius radius;
        public double minLength;
        public double minOverscrollLength;

        private ScrollMetrics _lastMetrics;
        private AxisDirection? _lastAxisDirection;

        public void update(ScrollMetrics metrics, AxisDirection axisDirection) {
            this._lastMetrics = metrics;
            this._lastAxisDirection = axisDirection;
            this.notifyListeners();
        }

        private Paint _paint {
            get {
                var paint = new Paint();
                paint.color = this.color.withOpacity(this.color.opacity * this.fadeoutOpacityAnimation.value);
                return paint;
            }
        }

        private double _getThumbX(Size size) {
            D.assert(this.textDirection != null);
            switch (textDirection) {
                case TextDirection.rtl:
                    return this.crossAxisMargin;
                case TextDirection.ltr:
                    return size.width - this.thickness - this.crossAxisMargin;
            }

            return 0;
        }

        private void _paintVerticalThumb(Canvas canvas, Size size, double thumbOffset, double thumbExtent) {
            Offset thumbOrigin = new Offset(_getThumbX(size), thumbOffset);
            Size thumbSize = new Size(this.thickness, thumbExtent);
            Rect thumbRect = thumbOrigin & thumbSize;
            if (this.radius == null) {
                canvas.drawRect(thumbRect, _paint);
            }
            else {
                canvas.drawRRect(RRect.fromRectAndRadius(thumbRect, this.radius), _paint);
            }
        }

        private void _paintHorizontalThumb(Canvas canvas, Size size, double thumbOffset, double thumbExtent) {
            Offset thumbOrigin = new Offset(thumbOffset, size.height - this.thickness);
            Size thumbSize = new Size(thumbExtent, this.thickness);
            Rect thumbRect = thumbOrigin & thumbSize;
            if (this.radius == null) {
                canvas.drawRect(thumbRect, _paint);
            }
            else {
                canvas.drawRRect(RRect.fromRectAndRadius(thumbRect, this.radius), _paint);
            }
        }

        public delegate void painterDelegate(Canvas canvas, Size size, double thumbOffset, double thumbExtent);

        private void _paintThumb(
            double before,
            double inside,
            double after,
            double viewport,
            Canvas canvas,
            Size size,
            painterDelegate painter
        ) {
            double thumbExtent = Math.Min(viewport, this.minOverscrollLength);

            if (before + inside + after > 0.0) {
                double fractionVisible = inside / (before + inside + after);
                thumbExtent = Math.Max(
                    thumbExtent,
                    viewport * fractionVisible - 2 * this.mainAxisMargin
                );

                if (before != 0.0 && after != 0.0) {
                    thumbExtent = Math.Max(
                        this.minLength,
                        thumbExtent
                    );
                }
                else {
                    thumbExtent = Math.Max(
                        thumbExtent,
                        this.minLength * (((inside / viewport) - 0.8) / 0.2)
                    );
                }
            }

            double fractionPast = before / (before + after);
            double thumbOffset = (before + after > 0.0)
                ? fractionPast * (viewport - thumbExtent - 2 * this.mainAxisMargin) + this.mainAxisMargin
                : this.mainAxisMargin;

            painter(canvas, size, thumbOffset, thumbExtent);
        }

        public override void dispose() {
            this.fadeoutOpacityAnimation.removeListener(this.notifyListeners);
            base.dispose();
        }


        public void paint(Canvas canvas, Size size) {
            if (this._lastAxisDirection == null
                || this._lastMetrics == null
                || this.fadeoutOpacityAnimation.value == 0.0) {
                return;
            }

            switch (_lastAxisDirection) {
                case AxisDirection.down:
                    _paintThumb(this._lastMetrics.extentBefore(), this._lastMetrics.extentInside(),
                        this._lastMetrics.extentAfter(), size.height, canvas, size, this._paintVerticalThumb);
                    break;
                case AxisDirection.up:
                    _paintThumb(this._lastMetrics.extentAfter(), this._lastMetrics.extentInside(),
                        this._lastMetrics.extentBefore(), size.height, canvas, size, this._paintVerticalThumb);
                    break;
                case AxisDirection.right:
                    _paintThumb(this._lastMetrics.extentBefore(), this._lastMetrics.extentInside(),
                        this._lastMetrics.extentAfter(), size.width, canvas, size, this._paintHorizontalThumb);
                    break;
                case AxisDirection.left:
                    _paintThumb(this._lastMetrics.extentAfter(), this._lastMetrics.extentInside(),
                        this._lastMetrics.extentBefore(), size.width, canvas, size, this._paintHorizontalThumb);
                    break;
            }
        }

        public bool hitTest(Offset position) {
            return false;
        }

        public bool shouldRepaint(ICustomPainter oldRaw) {
            if (oldRaw is ScrollbarPainter old) {
                return this.color != old.color
                       || this.textDirection != old.textDirection
                       || this.thickness != old.thickness
                       || this.fadeoutOpacityAnimation != old.fadeoutOpacityAnimation
                       || this.mainAxisMargin != old.mainAxisMargin
                       || this.crossAxisMargin != old.crossAxisMargin
                       || this.radius != old.radius
                       || this.minLength != old.minLength;
            }

            return false;
        }
    }
}
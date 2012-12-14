using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

using MonoDevelop.Components;
using MonoDevelop.Ide;
using MonoDevelop.Ide.Gui;

namespace XamarinCanvas
{
	public abstract class CanvasElement : IComparable<CanvasElement>, Animatable
	{
		public bool InputTransparent { get; set; }
		public bool Sensative { get; set; }
		public bool Draggable { get; set; }
		public bool CanFocus { get; set; }
		public bool HasFocus { get; private set; }

		public double X { get; set; }
		public double Y { get; set; }
		public double Rotation { get; set; }
		public double Scale { get; set; }
		public double Depth { get; set; }

		internal Cairo.Matrix Transform { get {
				var result = new Cairo.Matrix ();
				result.InitIdentity ();
				result.Translate (X, Y);
				result.Rotate (Rotation);
				result.Scale (Scale, Scale);

				if (Parent != null)
					result.Multiply (Parent.Transform);

				return result;
			}
		}

		internal Cairo.Matrix InverseTransform { get {
				var result = Transform;
				result.Invert ();
				return result;
			}
		}


		public virtual IEnumerable<CanvasElement> Children {
			get {
				return Enumerable.Empty<CanvasElement> ();
			}
		}

		public CanvasElement Parent { get; set; }

		Canvas canvas;
		public Canvas Canvas {
			get {
				if (canvas == null && Parent != null)
					return Parent.Canvas;
				return canvas;
			}
			set {
				canvas = value;
			}
		}

		protected CanvasElement ()
		{
			Scale = 1;
			CanFocus = true;
		}

		public void LayoutOutline (Cairo.Context context)
		{
			OnLayoutOutline (context);
		}

		public void Render (Cairo.Context context)
		{
			OnRender (context);
		}

		public void MouseIn ()
		{
			OnMouseIn ();
		}

		public void MouseOut ()
		{
			OnMouseOut ();
		}

		public void MouseMotion (double x, double y, Gdk.ModifierType state)
		{
			OnMouseMotion (x, y, state);
		}

		public void ButtonPress (double x, double y, uint button, Gdk.ModifierType state) 
		{
			OnButtonPress (x, y, button, state);
		}

		public void ButtonRelease (double x, double y, uint button, Gdk.ModifierType state) 
		{
			OnButtonRelease (x, y, button, state);
		}

		public void FocusIn ()
		{
			HasFocus = true;
			OnFocusIn ();
		}

		public void FocusOut ()
		{
			HasFocus = false;
			OnFocusOut ();
		}

		public void Clicked (double x, double y, Gdk.ModifierType state)
		{
			OnClicked (x, y, state);
		}

		public void KeyPress (Gdk.EventKey evnt)
		{
			OnKeyPress (evnt);
		}

		public void KeyRelease (Gdk.EventKey evnt)
		{
			OnKeyRelease (evnt);
		}

		private Cairo.PointD GetPoint(double t, Cairo.PointD p0, Cairo.PointD p1, Cairo.PointD p2, Cairo.PointD p3)
		{
			double cx = 3 * (p1.X - p0.X);
			double cy = 3 * (p1.Y - p0.Y);
			
			double bx = 3 * (p2.X - p1.X) - cx;
			double by = 3 * (p2.Y - p1.Y) - cy;
			
			double ax = p3.X - p0.X - cx - bx;
			double ay = p3.Y - p0.Y - cy - by;
			
			double Cube = t * t * t;
			double Square = t * t;
			
			double resX = (ax * Cube) + (bx * Square) + (cx * t) + p0.X;
			double resY = (ay * Cube) + (by * Square) + (cy * t) + p0.Y;
			
			return new Cairo.PointD(resX, resY);
		}

		public void CurveTo (double x1, double y1, double x2, double y2, double x3, double y3, uint length = 250, Func<float, float> easing = null)
		{
			if (easing == null)
				easing = Easing.Linear;
			Cairo.PointD start = new Cairo.PointD (X, Y);
			Cairo.PointD p1 = new Cairo.PointD (x1, y1);
			Cairo.PointD p2 = new Cairo.PointD (x2, y2);
			Cairo.PointD end = new Cairo.PointD (x3, y3);
			new Animation (f => {
				var position = GetPoint (f, start, p1, p2, end);
				X = position.X;
				Y = position.Y;
			}, 0, 1, easing)
				.Commit (this, "MoveTo", 16, length);
		}

		public void MoveTo (double x, double y, uint length = 250, Func<float, float> easing = null)
		{
			if (easing == null)
				easing = Easing.Linear;
			new Animation ()
				.Insert (0, 1, new Animation (f => X = f, (float)X, (float)x, easing))
				.Insert (0, 1, new Animation (f => Y = f, (float)Y, (float)y, easing))
				.Commit (this, "MoveTo", 16, length);
		}

		public void RotateTo (double roatation, uint length = 250, Func<float, float> easing = null)
		{
			if (easing == null)
				easing = Easing.Linear;
			new Animation (f => Rotation = f, (float)Rotation, (float)roatation, easing)
				.Commit (this, "RotateTo", 16, length);
		}

		public void ScaleTo (double scale, uint length = 250, Func<float, float> easing = null)
		{
			if (easing == null)
				easing = Easing.Linear;
			new Animation (f => Scale = f, (float)Scale, (float)scale, easing)
				.Commit (this, "ScaleTo", 16, length);
		}

		public void CancelAnimations ()
		{
			// Fixme : needs to abort all animations
			this.AbortAnimation ("MoveTo");
			this.AbortAnimation ("RotateTo");
			this.AbortAnimation ("ScaleTo");
		}

		protected virtual void OnMouseIn () {}
		protected virtual void OnMouseOut () {}
		protected virtual void OnMouseMotion (double x, double y, Gdk.ModifierType state) {}
		protected virtual void OnButtonPress (double x, double y, uint button, Gdk.ModifierType state) {}
		protected virtual void OnButtonRelease (double x, double y, uint button, Gdk.ModifierType state) {}
		protected virtual void OnClicked (double x, double y, Gdk.ModifierType state) {}
		protected virtual void OnFocusIn () {}
		protected virtual void OnFocusOut () {}
		protected virtual void OnKeyPress (Gdk.EventKey evnt) {}
		protected virtual void OnKeyRelease (Gdk.EventKey evnt) {}

		protected abstract void OnLayoutOutline (Cairo.Context context);
		protected abstract void OnRender (Cairo.Context context);

		public void QueueDraw ()
		{
			if (Canvas != null) {
				Canvas.ChildNeedDraw ();
			}
		}

		#region IComparable implementation

		public int CompareTo (CanvasElement other)
		{
			return Depth.CompareTo (other.Depth);
		}

		#endregion
	}

	public class Canvas : Gtk.EventBox, Animatable
	{
		GroupCanvasElement rootElement;

		MouseTracker tracker;

		CanvasElement hoveredElement;
		CanvasElement HoveredElement {
			get {
				return hoveredElement;
			}
			set {
				if (value == hoveredElement)
					return;

				if (MouseGrabElement != null)
					return;

				var old = hoveredElement;
				hoveredElement = value;

				if (old != null)
					old.MouseOut ();


				if (hoveredElement != null)
					hoveredElement.MouseIn ();
			}
		}

		Gdk.Point DragOffset { get; set; }
		Gdk.Point DragStart { get; set; }
		bool dragging;

		CanvasElement mouseGrabElement;
		CanvasElement MouseGrabElement {
			get {
				return mouseGrabElement;
			}
			set {
				mouseGrabElement = value;
				HoveredElement = value;
			}
		}

		CanvasElement LastFocusedElement { get; set; }

		CanvasElement focusedElement;
		CanvasElement FocusedElement {
			get {
				return focusedElement;
			}
			set {
				if (value == focusedElement)
					return;

				var old = focusedElement;
				focusedElement = value;

				if (old != null)
					old.FocusOut ();

				if (focusedElement != null)
					focusedElement.FocusIn ();
			}
		}

		public Canvas ()
		{
			AppPaintable = true;
			CanFocus = true;
			rootElement = new GroupCanvasElement ();
			rootElement.Canvas = this;

			AddEvents ((int)(Gdk.EventMask.AllEventsMask));
			tracker = new MouseTracker (this);
		}

		public void AddElement (CanvasElement element)
		{
			rootElement.Add (element);
			QueueDraw ();
		}

		public void RemoveElement (CanvasElement element)
		{
			rootElement.Remove (element);
			QueueDraw ();
		}

		public void ChildNeedDraw ()
		{
			QueueDraw ();
			if (tracker.Hovered)
				HoveredElement = GetElementAt (tracker.MousePosition.X, tracker.MousePosition.Y);
		}

		CanvasElement GetElementAt (Cairo.Context context, CanvasElement root, double x, double y)
		{
			foreach (var element in root.Children.Reverse ()) {
				var result = GetElementAt (context, element, x, y);
				if (result != null)
					return result;
			}

			context.Save ();
			root.LayoutOutline (context);
			
			double dx = x;
			double dy = y;
			root.InverseTransform.TransformPoint (ref dx, ref dy);
			
			if (context.InFill (dx, dy)) {
				context.NewPath ();
				context.Restore ();
				return root;
			}
			context.NewPath ();
			context.Restore ();
			return null;
		}

		CanvasElement GetElementAt (double x, double y)
		{
			using (var context = Gdk.CairoHelper.Create (GdkWindow)) {
				return GetElementAt (context, rootElement, x, y);
			}
		}

		protected override bool OnMotionNotifyEvent (Gdk.EventMotion evnt)
		{
			double dx = evnt.X;
			double dy = evnt.Y;
			if (MouseGrabElement != null) {
				if (MouseGrabElement.Draggable && evnt.State.HasFlag (Gdk.ModifierType.Button1Mask)) {
					if (!dragging && (Math.Abs (DragStart.X - dx) > 5 || Math.Abs (DragStart.Y - dy) > 5))
						dragging = true;

					if (dragging) {
						MouseGrabElement.Parent.InverseTransform.TransformPoint (ref dx, ref dy);
						MouseGrabElement.X = dx - DragOffset.X;
						MouseGrabElement.Y = dy - DragOffset.Y;
						QueueDraw ();
					}
				} else {
					var point = TransformPoint (MouseGrabElement, evnt.X, evnt.Y);
					MouseGrabElement.MouseMotion (point.X, point.Y, evnt.State);
				}
			} else {
				HoveredElement = GetElementAt (evnt.X, evnt.Y);
				if (HoveredElement != null) {
					var point = TransformPoint (HoveredElement, evnt.X, evnt.Y);
					HoveredElement.MouseMotion (point.X, point.Y, evnt.State);
				}
			}

			return base.OnMotionNotifyEvent (evnt);
		}

		protected override bool OnEnterNotifyEvent (Gdk.EventCrossing evnt)
		{
			return base.OnEnterNotifyEvent (evnt);
		}

		protected override bool OnLeaveNotifyEvent (Gdk.EventCrossing evnt)
		{
			HoveredElement = null;
			return base.OnLeaveNotifyEvent (evnt);
		}

		Cairo.PointD TransformPoint (CanvasElement element, double x, double y)
		{
			element.InverseTransform.TransformPoint (ref x, ref y);
			return new Cairo.PointD (x, y);
		}

		protected override bool OnButtonPressEvent (Gdk.EventButton evnt)
		{
			int x = (int)evnt.X;
			int y = (int)evnt.Y;
			var element = GetElementAt (x, y); 

			HasFocus = true;

			MouseGrabElement = element;
			if (MouseGrabElement != null) {
				MouseGrabElement.CancelAnimations ();
				DragStart = new Gdk.Point (x, y);

				double dx = x;
				double dy = y;
				MouseGrabElement.Parent.InverseTransform.TransformPoint (ref dx, ref dy);
				DragOffset = new Gdk.Point ((int) (dx - MouseGrabElement.X), (int) (dy - MouseGrabElement.Y));

				var transformedPoint = TransformPoint (MouseGrabElement, x, y);
				MouseGrabElement.ButtonPress (transformedPoint.X, transformedPoint.Y, evnt.Button, evnt.State);
			}

			return true;
		}

		protected override bool OnButtonReleaseEvent (Gdk.EventButton evnt)
		{
			int x = (int)evnt.X;
			int y = (int)evnt.Y;
			var element = GetElementAt (x, y);
			if (element != null && element == MouseGrabElement && !dragging) {
				var point = TransformPoint (element, x, y);
				element.ButtonRelease (point.X, point.Y, evnt.Button, evnt.State);
				element.Clicked (point.X, point.Y, evnt.State);
				if (element.CanFocus)
					FocusedElement = element;
			}
			dragging = false;
			MouseGrabElement = null;
			return true;
		}

		protected override bool OnFocusInEvent (Gdk.EventFocus evnt)
		{
			if (LastFocusedElement != null)
				FocusedElement = LastFocusedElement;
			return base.OnFocusInEvent (evnt);
		}

		protected override bool OnFocusOutEvent (Gdk.EventFocus evnt)
		{
			if (FocusedElement != null)
				LastFocusedElement = FocusedElement;
			FocusedElement = null;
			return base.OnFocusOutEvent (evnt);
		}

		protected override bool OnKeyPressEvent (Gdk.EventKey evnt)
		{
			if (FocusedElement != null)
				FocusedElement.KeyPress (evnt);
			return base.OnKeyPressEvent (evnt);
		}

		protected override bool OnKeyReleaseEvent (Gdk.EventKey evnt)
		{
			if (FocusedElement != null)
				FocusedElement.KeyRelease (evnt);
			return base.OnKeyReleaseEvent (evnt);
		}

		void RenderElement (Cairo.Context context, CanvasElement element)
		{
			context.Save ();
			context.Transform (element.Transform);
			element.Render (context);
			context.Restore ();

			if (element.Children != null)
				foreach (var child in element.Children)
					RenderElement (context, child);
		}
		
		void Paint (Cairo.Context context)
		{
			context.Color = new Cairo.Color (0, 0, 0);
			context.Paint ();

			RenderElement (context, rootElement);
		}

		protected override bool OnExposeEvent (Gdk.EventExpose evnt)
		{
			using (var context = Gdk.CairoHelper.Create (GdkWindow)) {
				Paint (context);
			}
			return true;
		}
	}
}


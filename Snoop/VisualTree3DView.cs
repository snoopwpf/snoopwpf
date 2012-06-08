// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using System.Windows.Media.Imaging;

namespace Snoop
{
	public class VisualTree3DView : Viewport3D
	{
		public VisualTree3DView(Visual visual)
		{
			DirectionalLight directionalLight1 = new DirectionalLight(Colors.White, new Vector3D(0, 0, 1));
			DirectionalLight directionalLight2 = new DirectionalLight(Colors.White, new Vector3D(0, 0, -1));

			double z = 0;
			Model3D model = this.ConvertVisualToModel3D(visual, ref z);

			Model3DGroup group = new Model3DGroup();
			group.Children.Add(directionalLight1);
			group.Children.Add(directionalLight2);
			group.Children.Add(model);
			this.zScaleTransform = new ScaleTransform3D();
			group.Transform = this.zScaleTransform;

			ModelVisual3D modelVisual = new ModelVisual3D();
			modelVisual.Content = group;

			Rect3D bounds = model.Bounds;
			double fieldOfView = 45;
			Point3D lookAtPoint = new Point3D(bounds.X + bounds.SizeX / 2, bounds.Y + bounds.SizeY / 2, bounds.Z + bounds.SizeZ / 2);
			double cameraDistance = 0.5 * bounds.SizeX / Math.Tan(0.5 * fieldOfView * Math.PI / 180);
			Point3D position = lookAtPoint - new Vector3D(0, 0, cameraDistance);
			Camera camera = new PerspectiveCamera(position, new Vector3D(0, 0, 1), new Vector3D(0, -1, 0), fieldOfView);

			this.zScaleTransform.CenterZ = lookAtPoint.Z;

			this.Children.Add(modelVisual);
			this.Camera = camera;
			this.ClipToBounds = false;
			this.Width = 500;
			this.Height = 500;

			this.trackballBehavior = new TrackballBehavior(this, lookAtPoint);
		}

		public double ZScale
		{
			get { return this.zScaleTransform.ScaleZ; }
			set { this.zScaleTransform.ScaleZ = value; }
		}

		public void Reset()
		{
			this.trackballBehavior.Reset();
			this.ZScale = 1;
		}

		private Model3D ConvertVisualToModel3D(Visual visual, ref double z)
		{
			Model3D model = null;
			Rect bounds = VisualTreeHelper.GetContentBounds(visual);
			Viewport3D viewport = visual as Viewport3D;
			if (viewport != null)
			{
				bounds = new Rect(viewport.RenderSize);
			}
			if (this.includeEmptyVisuals)
			{
				bounds.Union(VisualTreeHelper.GetDescendantBounds(visual));
			}
			if (!bounds.IsEmpty && bounds.Width > 0 && bounds.Height > 0)
			{
				MeshGeometry3D mesh = new MeshGeometry3D();
				mesh.Positions.Add(new Point3D(bounds.Left, bounds.Top, z));
				mesh.Positions.Add(new Point3D(bounds.Right, bounds.Top, z));
				mesh.Positions.Add(new Point3D(bounds.Right, bounds.Bottom, z));
				mesh.Positions.Add(new Point3D(bounds.Left, bounds.Bottom, z));
				mesh.TextureCoordinates.Add(new Point(0, 0));
				mesh.TextureCoordinates.Add(new Point(1, 0));
				mesh.TextureCoordinates.Add(new Point(1, 1));
				mesh.TextureCoordinates.Add(new Point(0, 1));
				mesh.Normals.Add(new Vector3D(0, 0, 1));
				mesh.Normals.Add(new Vector3D(0, 0, 1));
				mesh.Normals.Add(new Vector3D(0, 0, 1));
				mesh.Normals.Add(new Vector3D(0, 0, 1));
				mesh.TriangleIndices = new Int32Collection(new int[] { 0, 1, 2, 2, 3, 0 });
				mesh.Freeze();

				Brush brush = this.MakeBrushFromVisual(visual, bounds);
				DiffuseMaterial material = new DiffuseMaterial(brush);
				material.Freeze();

				model = new GeometryModel3D(mesh, material);
				((GeometryModel3D)model).BackMaterial = material;

				z -= 1;
			}

			int childrenCount = VisualTreeHelper.GetChildrenCount(visual);
			if (childrenCount > 0)
			{
				Model3DGroup group = new Model3DGroup();
				if (model != null)
				{
					group.Children.Add(model);
				}
				for (int i = 0; i < childrenCount; i++)
				{
					Visual childVisual = VisualTreeHelper.GetChild(visual, i) as Visual;
					if (childVisual != null)
					{
						Model3D childModel = this.ConvertVisualToModel3D(childVisual, ref z);
						if (childModel != null)
						{
							group.Children.Add(childModel);
						}
					}
				}
				model = group;
			}

			if (model != null)
			{
				Transform transform = VisualTreeHelper.GetTransform(visual);
				Matrix matrix = (transform == null ? Matrix.Identity : transform.Value);
				Vector offset = VisualTreeHelper.GetOffset(visual);
				matrix.Translate(offset.X, offset.Y);
				if (!matrix.IsIdentity)
				{
					Matrix3D matrix3D = new Matrix3D(matrix.M11, matrix.M12, 0, 0, matrix.M21, matrix.M22, 0, 0, 0, 0, 1, 0, matrix.OffsetX, matrix.OffsetY, 0, 1);
					Transform3D transform3D = new MatrixTransform3D(matrix3D);
					transform3D.Freeze();
					model.Transform = transform3D;
				}
				model.Freeze();
			}

			return model;
		}
		private Brush MakeBrushFromVisual(Visual visual, Rect bounds)
		{
			Viewport3D viewport = visual as Viewport3D;
			if (viewport == null)
			{
				Drawing drawing = VisualTreeHelper.GetDrawing(visual);
				if (this.drawOutlines)
				{
					bounds.Inflate(VisualTree3DView.OutlinePen.Thickness / 2, VisualTree3DView.OutlinePen.Thickness / 2);
				}

				Matrix offsetMatrix = new Matrix(1, 0, 0, 1, -bounds.Left, -bounds.Top);
				MatrixTransform offsetMatrixTransform = new MatrixTransform(offsetMatrix);
				offsetMatrixTransform.Freeze();

				DrawingVisual drawingVisual = new DrawingVisual();
				DrawingContext drawingContext = drawingVisual.RenderOpen();
				drawingContext.PushTransform(offsetMatrixTransform);
				if (this.drawOutlines)
				{
					drawingContext.DrawRectangle(null, VisualTree3DView.OutlinePen, bounds);
				}
				drawingContext.DrawDrawing(drawing);
				drawingContext.Pop();
				drawingContext.Close();

				visual = drawingVisual;
			}

			RenderTargetBitmap renderTargetBitmap = new RenderTargetBitmap((int)Math.Ceiling(bounds.Width), (int)Math.Ceiling(bounds.Height), 96, 96, PixelFormats.Default);
			if (viewport != null)
			{
				typeof(RenderTargetBitmap).GetMethod("RenderForBitmapEffect", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic).Invoke(renderTargetBitmap,
					new object[] { visual, Matrix.Identity, Rect.Empty });
			}
			else
			{
				renderTargetBitmap.Render(visual);
			}
			renderTargetBitmap.Freeze();
			ImageBrush imageBrush = new ImageBrush(renderTargetBitmap);
			imageBrush.Freeze();

			return imageBrush;
		}

		private bool drawOutlines = false;
		private bool includeEmptyVisuals = false;
		private TrackballBehavior trackballBehavior;
		private ScaleTransform3D zScaleTransform;

		private static Pen OutlinePen = new Pen(new SolidColorBrush(Color.FromArgb(128, 255, 0, 0)), 2);
	}
}

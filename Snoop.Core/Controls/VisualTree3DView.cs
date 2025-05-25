// (c) Copyright Cory Plotts.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

namespace Snoop.Controls;

using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Media.Media3D;
using Snoop.Infrastructure;

public class VisualTree3DView : Viewport3D
{
    private static readonly Pen outlinePen = new(new SolidColorBrush(Color.FromArgb(128, 255, 0, 0)), 2);

    private readonly bool drawOutlines = false;
    private readonly bool includeEmptyVisuals = false;
    private readonly TrackballBehavior? trackballBehavior;
    private readonly ScaleTransform3D zScaleTransform;

    public VisualTree3DView(Visual visual, int dpi)
    {
        var directionalLight1 = new DirectionalLight(Colors.White, new Vector3D(0, 0, 1));
        var directionalLight2 = new DirectionalLight(Colors.White, new Vector3D(0, 0, -1));

        double z = 0;
        var model = this.ConvertVisualToModel3D(visual, dpi, ref z);

        var group = new Model3DGroup();
        group.Children.Add(directionalLight1);
        group.Children.Add(directionalLight2);
        if (model is not null)
        {
            group.Children.Add(model);
        }

        this.zScaleTransform = new ScaleTransform3D();
        group.Transform = this.zScaleTransform;

        var modelVisual = new ModelVisual3D
        {
            Content = @group
        };

        if (model is not null)
        {
            var bounds = model.Bounds;
            const double fieldOfView = 45;
            var lookAtPoint = new Point3D(bounds.X + (bounds.SizeX / 2), bounds.Y + (bounds.SizeY / 2), bounds.Z + (bounds.SizeZ / 2));
            var cameraDistance = 0.5 * bounds.SizeX / Math.Tan(0.5 * fieldOfView * Math.PI / 180);
            var position = lookAtPoint - new Vector3D(0, 0, cameraDistance);
            Camera camera = new PerspectiveCamera(position, new Vector3D(0, 0, 1), new Vector3D(0, -1, 0), fieldOfView);

            this.zScaleTransform.CenterZ = lookAtPoint.Z;

            this.Children.Add(modelVisual);
            this.Camera = camera;
            this.trackballBehavior = new TrackballBehavior(this, lookAtPoint);
        }

        this.ClipToBounds = false;
        this.Width = 500;
        this.Height = 500;
    }

    public double ZScale
    {
        get { return this.zScaleTransform.ScaleZ; }
        set { this.zScaleTransform.ScaleZ = value; }
    }

    public void Reset()
    {
        this.trackballBehavior?.Reset();
        this.ZScale = 1;
    }

    private Model3D? ConvertVisualToModel3D(Visual? visual, int dpi, ref double z)
    {
        if (visual is null)
        {
            return null;
        }

        Model3D? model = null;
        var bounds = visual is UIElement { IsVisible: false } ? Rect.Empty : VisualTreeHelper.GetContentBounds(visual);

        if (visual is Viewport3D viewport3D)
        {
            bounds = new Rect(viewport3D.RenderSize);
        }

        if (this.includeEmptyVisuals)
        {
            bounds.Union(VisualTreeHelper.GetDescendantBounds(visual));
        }

        if (!bounds.IsEmpty && bounds.Width > 0 && bounds.Height > 0)
        {
            var mesh = new MeshGeometry3D();
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
            mesh.FreezeIfPossible();

            var brush = this.MakeBrushFromVisual(visual, bounds, dpi);
            var material = new DiffuseMaterial(brush);
            material.FreezeIfPossible();

            model = new GeometryModel3D(mesh, material);
            ((GeometryModel3D)model).BackMaterial = material;

            z -= 1;
        }

        var childrenCount = VisualTreeHelper.GetChildrenCount(visual);

        if (childrenCount > 0)
        {
            var group = new Model3DGroup();

            if (model is not null)
            {
                group.Children.Add(model);
            }

            for (var i = 0; i < childrenCount; i++)
            {
                if (VisualTreeHelper.GetChild(visual, i) is Visual childVisual)
                {
                    var childModel = this.ConvertVisualToModel3D(childVisual, dpi, ref z);
                    if (childModel is not null)
                    {
                        group.Children.Add(childModel);
                    }
                }
            }

            model = group;
        }

        if (model is not null)
        {
            var transform = VisualTreeHelper.GetTransform(visual);
            var matrix = transform?.Value ?? Matrix.Identity;
            var offset = VisualTreeHelper.GetOffset(visual);
            matrix.Translate(offset.X, offset.Y);

            if (!matrix.IsIdentity)
            {
                var matrix3D = new Matrix3D(matrix.M11, matrix.M12, 0, 0, matrix.M21, matrix.M22, 0, 0, 0, 0, 1, 0, matrix.OffsetX, matrix.OffsetY, 0, 1);
                Transform3D transform3D = new MatrixTransform3D(matrix3D);
                transform3D.FreezeIfPossible();
                model.Transform = transform3D;
            }

            model.FreezeIfPossible();
        }

        return model;
    }

    private Brush MakeBrushFromVisual(Visual visual, Rect bounds, int dpi)
    {
        var viewport3D = visual as Viewport3D;

        if (viewport3D is null)
        {
            var drawing = VisualTreeHelper.GetDrawing(visual);

            if (this.drawOutlines)
            {
                bounds.Inflate(outlinePen.Thickness / 2, outlinePen.Thickness / 2);
            }

            var offsetMatrix = new Matrix(1, 0, 0, 1, -bounds.Left, -bounds.Top);
            var offsetMatrixTransform = new MatrixTransform(offsetMatrix);
            offsetMatrixTransform.FreezeIfPossible();

            var drawingVisual = new DrawingVisual();

            using (var drawingContext = drawingVisual.RenderOpen())
            {
                drawingContext.PushTransform(offsetMatrixTransform);

                if (this.drawOutlines)
                {
                    drawingContext.DrawRectangle(null, outlinePen, bounds);
                }

                drawingContext.DrawDrawing(drawing);
                drawingContext.Pop();
            }

            visual = drawingVisual;
        }

        var renderTargetBitmap = VisualCaptureUtil.RenderVisual(visual, bounds.Size, dpi, dpi, viewport3D: viewport3D);
        renderTargetBitmap.FreezeIfPossible();
        var imageBrush = new ImageBrush(renderTargetBitmap);
        imageBrush.FreezeIfPossible();

        return imageBrush;
    }
}
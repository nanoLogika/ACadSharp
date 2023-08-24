﻿using ACadSharp.Entities;
using CSMath;
using System;
using System.Linq;

namespace ACadSharp.IO.DXF
{
	internal abstract partial class DxfSectionWriterBase
	{
		protected void writeEntity<T>(T entity)
			where T : Entity
		{
			//TODO: Implement complex entities in a separated branch
			switch (entity)
			{
				case Hatch:
					this.notify($"Entity type not implemented : {entity.GetType().FullName}", NotificationType.NotImplemented);
					return;
			}

			this._writer.Write(DxfCode.Start, entity.ObjectName);

			this.writeCommonObjectData(entity);

			this.writeCommonEntityData(entity);

			switch (entity)
			{
				case Arc arc:
					this.writeArc(arc);
					break;
				case Circle circle:
					this.writeCircle(circle);
					break;
				case Ellipse ellipse:
					this.writeEllipse(ellipse);
					break;
				case Hatch hatch:
					this.writeHatch(hatch);
					break;
				case Insert insert:
					this.writeInsert(insert);
					break;
				case Line line:
					this.writeLine(line);
					break;
				case LwPolyline lwPolyline:
					this.writeLwPolyline(lwPolyline);
					break;
				case Point point:
					this.writePoint(point);
					break;
				case Polyline polyline:
					this.writePolyline(polyline);
					break;
				case TextEntity text:
					this.writeTextEntity(text);
					break;
				case Vertex vertex:
					this.writeVertex(vertex);
					break;
				default:
					throw new NotImplementedException($"Entity not implemented {entity.GetType().FullName}");
			}
		}

		private void writeArc(Arc arc)
		{
			DxfClassMap map = DxfClassMap.Create<Arc>();

			this.writeCircle(arc);

			this._writer.Write(DxfCode.Subclass, DxfSubclassMarker.Arc);

			this._writer.Write(50, arc.StartAngle, map);
			this._writer.Write(51, arc.EndAngle, map);
		}

		private void writeCircle(Circle circle)
		{
			DxfClassMap map = DxfClassMap.Create<Circle>();

			this._writer.Write(DxfCode.Subclass, DxfSubclassMarker.Circle);

			this._writer.Write(10, circle.Center, map);

			this._writer.Write(39, circle.Thickness, map);
			this._writer.Write(40, circle.Radius, map);

			this._writer.Write(210, circle.Normal, map);
		}

		private void writeHatch(Hatch hatch)
		{
			DxfClassMap map = DxfClassMap.Create<Hatch>();

			this._writer.Write(DxfCode.Subclass, DxfSubclassMarker.Hatch);

			this._writer.Write(10, 0, map);
			this._writer.Write(20, 0, map);
			this._writer.Write(30, hatch.Elevation, map);

			this._writer.Write(210, hatch.Normal.X, map);

			this._writer.Write(2, hatch.Pattern.Name, map);

			this._writer.Write(70, hatch.IsSolid ? (short)1 : (short)0, map);
			this._writer.Write(71, hatch.IsAssociative ? (short)1 : (short)0, map);

			this._writer.Write(91, hatch.Paths.Count, map);
			foreach (var path in hatch.Paths)
			{
				this.writeBoundaryPath(path);
			}

			this.writeHatchPattern(hatch.Pattern);
		}

		private void writeBoundaryPath(Hatch.BoundaryPath path)
		{
			this._writer.Write(92, (int)path.Flags);

			if (!path.Flags.HasFlag(BoundaryPathFlags.Polyline))
			{
				this._writer.Write(93, path.Edges.Count);
			}

			foreach (Hatch.BoundaryPath.Edge edge in path.Edges)
			{
				this.writeHatchBoundaryPathEdge(edge);
			}

			//TODO: Check how this entities are handled
			this._writer.Write(97, path.Entities.Count);
			foreach (Entity entity in path.Entities)
			{
				this._writer.Write(330, entity.Handle);
			}
		}

		private void writeHatchBoundaryPathEdge(Hatch.BoundaryPath.Edge edge)
		{
			this._writer.Write(72, edge.Type);

			switch (edge)
			{
				case Hatch.BoundaryPath.Arc arc:
					this._writer.Write(10, arc.Center);
					this._writer.Write(40, arc.Radius);
					this._writer.Write(50, arc.StartAngle);
					this._writer.Write(51, arc.EndAngle);
					this._writer.Write(73, arc.CounterClockWise ? (short)1 : (short)0);
					break;
				case Hatch.BoundaryPath.Ellipse ellipse:
					this._writer.Write(10, ellipse.Center);
					this._writer.Write(11, ellipse.MajorAxisEndPoint);
					this._writer.Write(40, ellipse.MinorToMajorRatio);
					this._writer.Write(50, ellipse.StartAngle);
					this._writer.Write(51, ellipse.EndAngle);
					this._writer.Write(73, ellipse.CounterClockWise ? (short)1 : (short)0);
					break;
				case Hatch.BoundaryPath.Line line:
					this._writer.Write(10, line.Start);
					this._writer.Write(11, line.End);
					break;
				case Hatch.BoundaryPath.Polyline poly:
					this._writer.Write(73, poly.IsClosed ? (short)1 : (short)0);
					this._writer.Write(93, poly.Vertices.Count);
					foreach (var vertex in poly.Vertices)
					{
						this._writer.Write(10, vertex);
					}
					break;
				case Hatch.BoundaryPath.Spline spline:
					this._writer.Write(73, spline.Rational ? (short)1 : (short)0);
					this._writer.Write(74, spline.Periodic ? (short)1 : (short)0);

					this._writer.Write(94, (int)spline.Degree);
					this._writer.Write(95, spline.Knots.Count);
					this._writer.Write(96, spline.ControlPoints.Count);

					foreach (double knot in spline.Knots)
					{
						this._writer.Write(40, knot);
					}

					foreach (var point in spline.ControlPoints)
					{
						this._writer.Write(10, point.X);
						this._writer.Write(20, point.Y);
						if (spline.Rational)
						{
							this._writer.Write(42, point.Z);
						}
					}
					break;
				default:
					throw new ArgumentException($"Unknown Hatch.BoundaryPath.Edge type {edge.GetType().FullName}");
			}
		}

		private void writeHatchPattern(HatchPattern pattern)
		{
			//TODO: Hatch pattern need a refactor and a proper implementation
		}

		private void writeEllipse(Ellipse ellipse)
		{
			DxfClassMap map = DxfClassMap.Create<Ellipse>();

			this._writer.Write(DxfCode.Subclass, DxfSubclassMarker.Ellipse);

			this._writer.Write(10, ellipse.Center, map);

			this._writer.Write(11, ellipse.EndPoint, map);

			this._writer.Write(210, ellipse.Normal, map);

			this._writer.Write(39, ellipse.Thickness, map);
			this._writer.Write(40, ellipse.RadiusRatio, map);
			this._writer.Write(41, ellipse.StartParameter, map);
			this._writer.Write(42, ellipse.EndParameter, map);
		}

		private void writeInsert(Insert insert)
		{
			DxfClassMap map = DxfClassMap.Create<Insert>();

			this._writer.Write(DxfCode.Subclass, DxfSubclassMarker.Insert);

			this._writer.Write(2, insert.Block.Name, map);

			this._writer.Write(10, insert.InsertPoint, map);

			this._writer.Write(41, insert.XScale, map);
			this._writer.Write(42, insert.YScale, map);
			this._writer.Write(43, insert.ZScale, map);

			this._writer.Write(50, insert.Rotation, map);


			this._writer.Write(70, (short)insert.ColumnCount);
			this._writer.Write(71, (short)insert.RowCount);

			this._writer.Write(44, insert.ColumnSpacing);
			this._writer.Write(45, insert.RowSpacing);

			this._writer.Write(210, insert.Normal, map);

			if (insert.HasAttributes)
			{
				this._writer.Write(66, 1);

				//WARNING: Write extended data before attributes

				foreach (var att in insert.Attributes)
				{
					this.writeEntity(att);
				}

				this.writeSeqend(insert.Attributes.Seqend);
			}
		}

		private void writeLine(Line line)
		{
			DxfClassMap map = DxfClassMap.Create<Line>();

			this._writer.Write(DxfCode.Subclass, DxfSubclassMarker.Line);

			this._writer.Write(10, line.StartPoint, map);

			this._writer.Write(11, line.EndPoint, map);

			this._writer.Write(39, line.Thickness, map);

			this._writer.Write(210, line.Normal, map);
		}

		private void writeLwPolyline(LwPolyline polyline)
		{
			DxfClassMap map = DxfClassMap.Create<LwPolyline>();

			this._writer.Write(DxfCode.Subclass, DxfSubclassMarker.LwPolyline);

			this._writer.Write(90, polyline.Vertices.Count);
			this._writer.Write(70, (short)polyline.Flags);

			this._writer.Write(38, polyline.Elevation);
			this._writer.Write(39, polyline.Thickness);

			foreach (LwPolyline.Vertex v in polyline.Vertices)
			{
				this._writer.Write(10, v.Location);
				this._writer.Write(40, v.StartWidth);
				this._writer.Write(41, v.EndWidth);
				this._writer.Write(42, v.Bulge);
			}

			this._writer.Write(210, polyline.Normal, map);
		}

		private void writePoint(Point line)
		{
			DxfClassMap map = DxfClassMap.Create<Point>();

			this._writer.Write(DxfCode.Subclass, DxfSubclassMarker.Point);

			this._writer.Write(10, line.Location, map);

			this._writer.Write(39, line.Thickness, map);

			this._writer.Write(210, line.Normal, map);

			this._writer.Write(50, line.Rotation, map);
		}

		private void writePolyline(Polyline polyline)
		{
			DxfClassMap map;

			switch (polyline)
			{
				case Polyline2D:
					map = DxfClassMap.Create<Polyline2D>();
					break;
				case Polyline3D:
					map = DxfClassMap.Create<Polyline3D>();
					break;
				default:
					throw new NotImplementedException($"Polyline not implemented {polyline.GetType().FullName}");
			}

			this._writer.Write(DxfCode.Subclass, polyline.SubclassMarker);

			this._writer.Write(DxfCode.XCoordinate, 0);
			this._writer.Write(DxfCode.YCoordinate, 0);
			this._writer.Write(DxfCode.ZCoordinate, polyline.Elevation);

			this._writer.Write(70, (short)polyline.Flags, map);
			this._writer.Write(75, (short)polyline.SmoothSurface, map);

			this._writer.Write(210, polyline.Normal, map);

			foreach (Vertex v in polyline.Vertices)
			{
				this.writeEntity(v);
			}

			this.writeSeqend(polyline.Vertices.Seqend);
		}

		private void writeSeqend(Seqend seqend)
		{
			this._writer.Write(0, seqend.ObjectName);
			this._writer.Write(5, seqend.Handle);
			this._writer.Write(330, seqend.Owner.Handle);
			this._writer.Write(DxfCode.Subclass, DxfSubclassMarker.Entity);
			this._writer.Write(8, seqend.Layer.Name);
		}

		private void writeTextEntity(TextEntity text)
		{
			DxfClassMap map = DxfClassMap.Create<TextEntity>();

			this._writer.Write(DxfCode.Subclass, DxfSubclassMarker.Text);

			this._writer.Write(1, text.Value, map);

			this._writer.Write(10, text.InsertPoint, map);

			this._writer.Write(40, text.Height, map);

			if (text.WidthFactor != 1.0)
			{
				this._writer.Write(41, text.WidthFactor, map);
			}

			if (text.Rotation != 0.0)
			{
				this._writer.Write(50, text.Rotation, map);
			}

			if (text.ObliqueAngle != 0.0)
			{
				this._writer.Write(51, text.ObliqueAngle, map);
			}

			if (text.Style != null)
			{
				//TODO: Implement text style in the writer
				//this._writer.Write(7, text.Style.Name);
			}

			this._writer.Write(11, text.AlignmentPoint, map);

			this._writer.Write(210, text.Normal, map);

			if (text.Mirror != 0)
			{
				this._writer.Write(71, text.Mirror, map);
			}
			if (text.HorizontalAlignment != 0)
			{
				this._writer.Write(72, text.HorizontalAlignment, map);
			}

			if (text.GetType() == typeof(TextEntity))
			{
				this._writer.Write(DxfCode.Subclass, DxfSubclassMarker.Text);

				if (text.VerticalAlignment != 0)
				{
					this._writer.Write(73, text.VerticalAlignment, map);
				}
			}

			if (text is AttributeBase)
			{
				switch (text)
				{
					case AttributeEntity att:
						this._writer.Write(DxfCode.Subclass, DxfSubclassMarker.Attribute);
						this.writeAttributeBase(att);
						break;
					case AttributeDefinition attdef:
						this._writer.Write(DxfCode.Subclass, DxfSubclassMarker.AttributeDefinition);
						this._writer.Write(3, attdef.Prompt, DxfClassMap.Create<AttributeDefinition>());
						this.writeAttributeBase(attdef);
						break;
					default:
						throw new ArgumentException($"Unknown AttributeBase type {text.GetType().FullName}");
				}
			}
		}

		private void writeAttributeBase(AttributeBase att)
		{
			this._writer.Write(2, att.Tag);

			this._writer.Write(70, (short)att.Flags);
			this._writer.Write(73, (short)0);

			if (att.VerticalAlignment != 0)
			{
				this._writer.Write(74, (short)att.VerticalAlignment);
			}
		}

		private void writeVertex(Vertex v)
		{
			DxfClassMap map = DxfClassMap.Create<Vertex>();

			this._writer.Write(DxfCode.Subclass, DxfSubclassMarker.Vertex);
			this._writer.Write(DxfCode.Subclass, v.SubclassMarker);

			this._writer.Write(10, v.Location, map);

			this._writer.Write(40, v.StartWidth, map);
			this._writer.Write(41, v.EndWidth, map);
			this._writer.Write(42, v.Bulge, map);

			this._writer.Write(70, v.Flags, map);

			this._writer.Write(50, v.CurveTangent, map);
		}
	}
}

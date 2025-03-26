﻿using ACadSharp.Entities;
using ACadSharp.Tables;
using CSMath;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;

namespace ACadSharp.IO.SVG
{
	internal class SvgXmlWriter : XmlTextWriter
	{
		public event NotificationEventHandler OnNotification;

		public SvgConfiguration Configuration { get; } = new();

		public SvgXmlWriter(Stream w, Encoding encoding, SvgConfiguration configuration) : base(w, encoding)
		{
			this.Configuration = configuration;
		}

		public void WriteAttributeString(string localName, double value)
		{
			this.WriteAttributeString(localName, value.ToString(CultureInfo.InvariantCulture));
		}

		public void WriteBlock(BlockRecord record)
		{
			BoundingBox box = record.GetBoundingBox();
			this.startDocument(box);

			Transform transform = new Transform(-box.Min, new XYZ(1), XYZ.Zero);
			foreach (var e in record.Entities)
			{
				this.writeEntity(e);
			}

			this.endDocument();
		}

		private string colorSvg(Color color)
		{
			return $"rgb({color.R},{color.G},{color.B})";
		}

		private void endDocument()
		{
			this.WriteEndElement();
			this.WriteEndDocument();
			this.Close();
		}

		private void notify(string message, NotificationType type, Exception ex = null)
		{
			this.OnNotification?.Invoke(this, new NotificationEventArgs(message, type, ex));
		}

		private void startDocument(BoundingBox box)
		{
			this.WriteStartDocument();

			this.WriteStartElement("svg");
			this.WriteAttributeString("xmlns", "http://www.w3.org/2000/svg");

			this.WriteAttributeString("width", box.Max.X - box.Min.X);
			this.WriteAttributeString("height", box.Max.Y - box.Min.Y);

			this.WriteStartAttribute("viewBox");
			this.WriteValue(box.Min.X);
			this.WriteValue(" ");
			this.WriteValue(box.Min.Y);
			this.WriteValue(" ");
			this.WriteValue(box.Max.X - box.Min.X);
			this.WriteValue(" ");
			this.WriteValue(box.Max.Y - box.Min.Y);
			this.WriteEndAttribute();

			this.WriteAttributeString("transform", $"scale(1,-1)");
		}

		private string svgPoints(IEnumerable<IVector> points, Transform transform)
		{
			if (!points.Any())
			{
				return string.Empty;
			}

			StringBuilder sb = new StringBuilder();
			sb.Append(transform.ApplyTransform(points.First().Convert<XYZ>()).SvgPoint());
			foreach (IVector point in points.Skip(1))
			{
				sb.Append(' ');
				sb.Append(transform.ApplyTransform(point.Convert<XYZ>()).SvgPoint());
			}

			return sb.ToString();
		}

		private void writeArc(Arc arc, Transform transform)
		{
			//A rx ry rotation large-arc-flag sweep-flag x y

			this.WriteStartElement("polyline");

			this.writeEntityStyle(arc);

			IEnumerable<IVector> vertices = arc.PolygonalVertexes(256).OfType<IVector>();
			string pts = this.svgPoints(vertices, transform);
			this.WriteAttributeString("points", pts);
			this.WriteAttributeString("fill", "none");

			this.WriteEndElement();
		}

		private void writeCircle(Circle circle, Transform transform)
		{
			var loc = transform.ApplyTransform(circle.Center);

			this.WriteStartElement("circle");

			this.writeEntityStyle(circle);

			this.WriteAttributeString("r", circle.Radius);
			this.WriteAttributeString("cx", loc.X);
			this.WriteAttributeString("cy", loc.Y);

			this.WriteAttributeString("fill", "none");

			this.WriteEndElement();
		}

		private void writeEllipse(Ellipse ellipse, Transform transform)
		{
			this.WriteStartElement("polygon");

			this.writeEntityStyle(ellipse);

			IEnumerable<IVector> vertices = ellipse.PolygonalVertexes(256).OfType<IVector>();
			string pts = this.svgPoints(vertices, transform);
			this.WriteAttributeString("points", pts);
			this.WriteAttributeString("fill", "none");

			this.WriteEndElement();
		}

		private void writeEntity(Entity entity)
		{
			this.writeEntity(entity, new Transform());
		}

		private void writeEntity(Entity entity, Transform transform)
		{
			switch (entity)
			{
				case Arc arc:
					this.writeArc(arc, transform);
					break;
				case Line line:
					this.writeLine(line, transform);
					break;
				case Point point:
					this.writePoint(point, transform);
					break;
				case Circle circle:
					this.writeCircle(circle, transform);
					break;
				case Ellipse ellipse:
					this.writeEllipse(ellipse, transform);
					break;
				case IPolyline polyline:
					this.writePolyline(polyline, transform);
					break;
				default:
					this.notify($"[{entity.ObjectName}] Entity not implemented.", NotificationType.NotImplemented);
					break;
			}
		}

		private void writeEntityStyle(IEntity entity)
		{
			Color color = entity.GetActiveColor();

			this.WriteAttributeString("stroke", this.colorSvg(color));

			var lineWeight = entity.LineWeight;
			switch (lineWeight)
			{
				case LineweightType.ByLayer:
					lineWeight = entity.Layer.LineWeight;
					break;
			}

			this.WriteAttributeString("stroke-width", Configuration.GetLineWeightValue(lineWeight));
		}

		private void writeLine(Line line, Transform transform)
		{
			var start = transform.ApplyTransform(line.StartPoint);
			var end = transform.ApplyTransform(line.EndPoint);

			this.WriteStartElement("line");

			this.writeEntityStyle(line);

			this.WriteAttributeString("x1", start.X);
			this.WriteAttributeString("y1", start.Y);
			this.WriteAttributeString("x2", end.X);
			this.WriteAttributeString("y2", end.Y);

			this.WriteEndElement();
		}

		private void writePoint(Point point, Transform transform)
		{
			var loc = transform.ApplyTransform(point.Location);

			this.WriteStartElement("circle");

			this.writeEntityStyle(point);

			this.WriteAttributeString("r", this.Configuration.PointRadius);
			this.WriteAttributeString("cx", loc.X);
			this.WriteAttributeString("cy", loc.Y);

			this.WriteAttributeString("fill", this.colorSvg(point.GetActiveColor()));

			this.WriteEndElement();
		}

		private void writePolyline(IPolyline polyline, Transform transform)
		{
			if (polyline.IsClosed)
			{
				this.WriteStartElement("polygon");
			}
			else
			{
				this.WriteStartElement("polyline");
			}

			this.writeEntityStyle(polyline);

			var vertices = polyline.Vertices.Select(v => v.Location).ToList();

			string pts = this.svgPoints(polyline.Vertices.Select(v => v.Location), transform);
			this.WriteAttributeString("points", pts);
			this.WriteAttributeString("fill", "none");

			this.WriteEndElement();
		}
	}
}
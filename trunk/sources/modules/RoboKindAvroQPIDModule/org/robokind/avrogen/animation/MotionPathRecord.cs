// ------------------------------------------------------------------------------
// <auto-generated>
//    Generated by RoboKindChat.vshost.exe, version 0.9.0.0
//    Changes to this file may cause incorrect behavior and will be lost if code
//    is regenerated
// </auto-generated>
// ------------------------------------------------------------------------------
namespace org.robokind.avrogen.animation
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using Avro;
	using Avro.Specific;
	
	public partial class MotionPathRecord : ISpecificRecord, MotionPath
	{
		private static Schema _SCHEMA = Avro.Schema.Parse(@"{""type"":""record"",""name"":""MotionPathRecord"",""namespace"":""org.robokind.avrogen.animation"",""fields"":[{""name"":""name"",""type"":""string""},{""name"":""motionPathId"",""type"":""int""},{""name"":""startTime"",""type"":[""null"",""long""]},{""name"":""stopTime"",""type"":[""null"",""long""]},{""name"":""interpolator"",""type"":{""type"":""record"",""name"":""InterpolatorTypeRecord"",""namespace"":""org.robokind.avrogen.animation"",""fields"":[{""name"":""name"",""type"":""string""},{""name"":""versionNumber"",""type"":""string""}]}},{""name"":""controlPoints"",""type"":{""type"":""array"",""items"":{""type"":""record"",""name"":""ControlPointRecord"",""namespace"":""org.robokind.avrogen.animation"",""fields"":[{""name"":""time"",""type"":""long""},{""name"":""position"",""type"":""double""}]}}}]}");
		private string _name;
		private int _motionPathId;
		private System.Nullable<long> _startTime;
		private System.Nullable<long> _stopTime;
		private org.robokind.avrogen.animation.InterpolatorTypeRecord _interpolator;
		private IList<org.robokind.avrogen.animation.ControlPointRecord> _controlPoints;
		public virtual Schema Schema
		{
			get
			{
				return MotionPathRecord._SCHEMA;
			}
		}
		public string name
		{
			get
			{
				return this._name;
			}
			set
			{
				this._name = value;
			}
		}
		public int motionPathId
		{
			get
			{
				return this._motionPathId;
			}
			set
			{
				this._motionPathId = value;
			}
		}
		public System.Nullable<long> startTime
		{
			get
			{
				return this._startTime;
			}
			set
			{
				this._startTime = value;
			}
		}
		public System.Nullable<long> stopTime
		{
			get
			{
				return this._stopTime;
			}
			set
			{
				this._stopTime = value;
			}
		}
		public org.robokind.avrogen.animation.InterpolatorTypeRecord interpolator
		{
			get
			{
				return this._interpolator;
			}
			set
			{
				this._interpolator = value;
			}
		}
		public IList<org.robokind.avrogen.animation.ControlPointRecord> controlPoints
		{
			get
			{
				return this._controlPoints;
			}
			set
			{
				this._controlPoints = value;
			}
		}
		public virtual object Get(int fieldPos)
		{
			switch (fieldPos)
			{
			case 0: return this.name;
			case 1: return this.motionPathId;
			case 2: return this.startTime;
			case 3: return this.stopTime;
			case 4: return this.interpolator;
			case 5: return this.controlPoints;
			default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()");
			};
		}
		public virtual void Put(int fieldPos, object fieldValue)
		{
			switch (fieldPos)
			{
			case 0: this.name = (System.String)fieldValue; break;
			case 1: this.motionPathId = (System.Int32)fieldValue; break;
			case 2: this.startTime = (System.Nullable<long>)fieldValue; break;
			case 3: this.stopTime = (System.Nullable<long>)fieldValue; break;
			case 4: this.interpolator = (org.robokind.avrogen.animation.InterpolatorTypeRecord)fieldValue; break;
			case 5: this.controlPoints = (IList<org.robokind.avrogen.animation.ControlPointRecord>)fieldValue; break;
			default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
			};
		}
	}
}
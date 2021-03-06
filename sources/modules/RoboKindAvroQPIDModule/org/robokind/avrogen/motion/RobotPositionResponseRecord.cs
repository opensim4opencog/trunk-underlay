// ------------------------------------------------------------------------------
// <auto-generated>
//    Generated by RoboKindChat.vshost.exe, version 0.9.0.0
//    Changes to this file may cause incorrect behavior and will be lost if code
//    is regenerated
// </auto-generated>
// ------------------------------------------------------------------------------
namespace org.robokind.avrogen.motion
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using Avro;
	using Avro.Specific;
	
	public partial class RobotPositionResponseRecord : ISpecificRecord, RobotPositionResponse
	{
		private static Schema _SCHEMA = Avro.Schema.Parse(@"{""type"":""record"",""name"":""RobotPositionResponseRecord"",""namespace"":""org.robokind.avrogen.motion"",""fields"":[{""name"":""responseHeader"",""type"":{""type"":""record"",""name"":""RobotResponseHeaderRecord"",""namespace"":""org.robokind.avrogen.motion"",""fields"":[{""name"":""robotId"",""type"":""string""},{""name"":""requestSourceId"",""type"":""string""},{""name"":""requestDestinationId"",""type"":""string""},{""name"":""requestType"",""type"":""string""},{""name"":""requestTimestampMillisecUTC"",""type"":""long""},{""name"":""responseTimestampMillisecUTC"",""type"":""long""}]}},{""name"":""positionResponse"",""type"":{""type"":""record"",""name"":""RobotPositionMapRecord"",""namespace"":""org.robokind.avrogen.motion"",""fields"":[{""name"":""jointPositions"",""type"":{""type"":""array"",""items"":{""type"":""record"",""name"":""JointPositionRecord"",""namespace"":""org.robokind.avrogen.motion"",""fields"":[{""name"":""jointId"",""type"":{""type"":""record"",""name"":""JointIdRecord"",""namespace"":""org.robokind.avrogen.motion"",""fields"":[{""name"":""robotId"",""type"":""string""},{""name"":""jointId"",""type"":""int""}]}},{""name"":""normalizedPosition"",""type"":""double""}]}}}]}}]}");
		private org.robokind.avrogen.motion.RobotResponseHeaderRecord _responseHeader;
		private org.robokind.avrogen.motion.RobotPositionMapRecord _positionResponse;
		public virtual Schema Schema
		{
			get
			{
				return RobotPositionResponseRecord._SCHEMA;
			}
		}
		public org.robokind.avrogen.motion.RobotResponseHeaderRecord responseHeader
		{
			get
			{
				return this._responseHeader;
			}
			set
			{
				this._responseHeader = value;
			}
		}
		public org.robokind.avrogen.motion.RobotPositionMapRecord positionResponse
		{
			get
			{
				return this._positionResponse;
			}
			set
			{
				this._positionResponse = value;
			}
		}
		public virtual object Get(int fieldPos)
		{
			switch (fieldPos)
			{
			case 0: return this.responseHeader;
			case 1: return this.positionResponse;
			default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Get()");
			};
		}
		public virtual void Put(int fieldPos, object fieldValue)
		{
			switch (fieldPos)
			{
			case 0: this.responseHeader = (org.robokind.avrogen.motion.RobotResponseHeaderRecord)fieldValue; break;
			case 1: this.positionResponse = (org.robokind.avrogen.motion.RobotPositionMapRecord)fieldValue; break;
			default: throw new AvroRuntimeException("Bad index " + fieldPos + " in Put()");
			};
		}
	}
}

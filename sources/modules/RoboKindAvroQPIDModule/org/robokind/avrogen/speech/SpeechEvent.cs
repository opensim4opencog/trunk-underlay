// ------------------------------------------------------------------------------
// <auto-generated>
//    Generated by RoboKindChat.vshost.exe, version 0.9.0.0
//    Changes to this file may cause incorrect behavior and will be lost if code
//    is regenerated
// </auto-generated>
// ------------------------------------------------------------------------------
namespace org.robokind.avrogen.speech
{
	using System;
	using System.Collections.Generic;
	using System.Text;
	using Avro;
	using Avro.Specific;
	
	public interface SpeechEvent
	{
		Schema Schema
		{
			get;
		}
		string eventType
		{
			get;
		}
		long streamNumber
		{
			get;
		}
		int textPosition
		{
			get;
		}
		int textLength
		{
			get;
		}
		int currentData
		{
			get;
		}
		int nextData
		{
			get;
		}
		string stringData
		{
			get;
		}
		int duration
		{
			get;
		}
	}
}

using Apache.Qpid.Buffer;
using System.Text;

namespace Apache.Qpid.Framing
{
  ///
  /// This class is autogenerated
  /// Do not modify.
  ///
  /// @author Code Generator Script by robert.j.greig@jpmorgan.com
  public class TestTableOkBody : AMQMethodBody , IEncodableAMQDataBlock
  {
    public const int CLASS_ID = 120; 	
    public const int METHOD_ID = 31; 	

    public ulong IntegerResult;    
    public byte[] StringResult;    
     

    protected override ushort Clazz
    {
        get
        {
            return 120;
        }
    }
   
    protected override ushort Method
    {
        get
        {
            return 31;
        }
    }

    protected override uint BodySize
    {
    get
    {
        
        return (uint)
        8 /*IntegerResult*/+
            4 + (uint) (StringResult == null ? 0 : StringResult.Length)		 
        ;
         
    }
    }

    protected override void WriteMethodPayload(ByteBuffer buffer)
    {
        buffer.Put(IntegerResult);
            EncodingUtils.WriteLongstr(buffer, StringResult);
            		 
    }

    protected override void PopulateMethodBodyFromBuffer(ByteBuffer buffer)
    {
        IntegerResult = buffer.GetUInt64();
        StringResult = EncodingUtils.ReadLongstr(buffer);
        		 
    }

    public override string ToString()
    {
        StringBuilder buf = new StringBuilder(base.ToString());
        buf.Append(" IntegerResult: ").Append(IntegerResult);
        buf.Append(" StringResult: ").Append(StringResult);
         
        return buf.ToString();
    }

    public static AMQFrame CreateAMQFrame(ushort channelId, ulong IntegerResult, byte[] StringResult)
    {
        TestTableOkBody body = new TestTableOkBody();
        body.IntegerResult = IntegerResult;
        body.StringResult = StringResult;
        		 
        AMQFrame frame = new AMQFrame();
        frame.Channel = channelId;
        frame.BodyFrame = body;
        return frame;
    }
} 
}
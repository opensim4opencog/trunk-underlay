
using Apache.Qpid.Buffer;
using System.Text;

namespace Apache.Qpid.Framing
{
  ///
  /// This class is autogenerated
  /// Do not modify.
  ///
  /// @author Code Generator Script by robert.j.greig@jpmorgan.com
  public class TestStringBody : AMQMethodBody , IEncodableAMQDataBlock
  {
    public const int CLASS_ID = 120; 	
    public const int METHOD_ID = 20; 	

    public string String1;    
    public byte[] String2;    
    public byte Operation;    
     

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
            return 20;
        }
    }

    protected override uint BodySize
    {
    get
    {
        
        return (uint)
        (uint)EncodingUtils.EncodedShortStringLength(String1)+
            4 + (uint) (String2 == null ? 0 : String2.Length)+
            1 /*Operation*/		 
        ;
         
    }
    }

    protected override void WriteMethodPayload(ByteBuffer buffer)
    {
        EncodingUtils.WriteShortStringBytes(buffer, String1);
            EncodingUtils.WriteLongstr(buffer, String2);
            buffer.Put(Operation);
            		 
    }

    protected override void PopulateMethodBodyFromBuffer(ByteBuffer buffer)
    {
        String1 = EncodingUtils.ReadShortString(buffer);
        String2 = EncodingUtils.ReadLongstr(buffer);
        Operation = buffer.GetByte();
        		 
    }

    public override string ToString()
    {
        StringBuilder buf = new StringBuilder(base.ToString());
        buf.Append(" String1: ").Append(String1);
        buf.Append(" String2: ").Append(String2);
        buf.Append(" Operation: ").Append(Operation);
         
        return buf.ToString();
    }

    public static AMQFrame CreateAMQFrame(ushort channelId, string String1, byte[] String2, byte Operation)
    {
        TestStringBody body = new TestStringBody();
        body.String1 = String1;
        body.String2 = String2;
        body.Operation = Operation;
        		 
        AMQFrame frame = new AMQFrame();
        frame.Channel = channelId;
        frame.BodyFrame = body;
        return frame;
    }
} 
}
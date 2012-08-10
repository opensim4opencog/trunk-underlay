
using Apache.Qpid.Buffer;
using System.Text;

namespace Apache.Qpid.Framing
{
  ///
  /// This class is autogenerated
  /// Do not modify.
  ///
  /// @author Code Generator Script by robert.j.greig@jpmorgan.com
  public class ConnectionSecureBody : AMQMethodBody , IEncodableAMQDataBlock
  {
    public const int CLASS_ID = 10; 	
    public const int METHOD_ID = 20; 	

    public byte[] Challenge;    
     

    protected override ushort Clazz
    {
        get
        {
            return 10;
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
        4 + (uint) (Challenge == null ? 0 : Challenge.Length)		 
        ;
         
    }
    }

    protected override void WriteMethodPayload(ByteBuffer buffer)
    {
        EncodingUtils.WriteLongstr(buffer, Challenge);
            		 
    }

    protected override void PopulateMethodBodyFromBuffer(ByteBuffer buffer)
    {
        Challenge = EncodingUtils.ReadLongstr(buffer);
        		 
    }

    public override string ToString()
    {
        StringBuilder buf = new StringBuilder(base.ToString());
        buf.Append(" Challenge: ").Append(Challenge);
         
        return buf.ToString();
    }

    public static AMQFrame CreateAMQFrame(ushort channelId, byte[] Challenge)
    {
        ConnectionSecureBody body = new ConnectionSecureBody();
        body.Challenge = Challenge;
        		 
        AMQFrame frame = new AMQFrame();
        frame.Channel = channelId;
        frame.BodyFrame = body;
        return frame;
    }
} 
}
using UnityEngine;
using UnityEngine.Networking;

public class SerializedNetworkMessage : MessageBase{
	public byte[] bytes;

	public SerializedNetworkMessage() : this(null){}

	public SerializedNetworkMessage(byte[] bytes){
		this.bytes = bytes != null ? bytes : new byte[0];
	}

	// De-serialize the contents of the reader into this message
	public override void Deserialize(NetworkReader reader){
		this.bytes = reader.ReadBytesAndSize();
	}

	// Serialize the contents of this message into the writer
	public override void Serialize(NetworkWriter writer){
		writer.WriteBytesFull(this.bytes != null ? this.bytes : new byte[0]);
	}
}

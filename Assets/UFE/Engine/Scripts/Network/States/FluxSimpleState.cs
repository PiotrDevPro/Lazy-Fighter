using UnityEngine;
using System;
using System.IO;

[Serializable]
public struct FluxSimpleState : IEquatable<FluxSimpleState>{
	#region public class definitions
	public struct PlayerInformation : IEquatable<PlayerInformation>{
		public float life;
		public float gauge;
		public Vector3 position;


		public PlayerInformation(float life, float gauge, Vector3 position){
			this.life = life;
			this.gauge = gauge;
			this.position = position;
		}

		public override int GetHashCode (){
			unchecked{
				return 11 * this.life.GetHashCode() + 13 * this.gauge.GetHashCode() + 17 * this.position.GetHashCode();
			}
		}

		public override bool Equals (object obj){
			if (obj is PlayerInformation){
				return this.Equals((PlayerInformation)obj);
			}
			return false;
		}

		public bool Equals(PlayerInformation other){
			return this.life == other.life && this.gauge == other.gauge && Vector3.Equals(this.position, other.position);
		}

		public override string ToString (){
			return string.Format(
				"[PlayerInformation | position = ({0}, {1}, {2}) | life = {3} | gauge = {4}]", 
				this.position.x,
				this.position.y,
				this.position.z,
				this.life, 
				this.gauge
			);
		}
	}
	#endregion

	#region public instance properties
	public PlayerInformation p1;
	public PlayerInformation p2;

	public long frame;

	#endregion

	#region public instance constructors
	public FluxSimpleState(PlayerInformation p1, PlayerInformation p2, long frame){
		this.p1 = p1;
		this.p2 = p2;
		this.frame = frame;
	}

	public FluxSimpleState(FluxStates state){
		this.p1 = new PlayerInformation((float)state.player1.life, (float)state.player1.gauge,state.player1.shellTransform.position);
		this.p2 = new PlayerInformation((float)state.player2.life, (float)state.player2.gauge,state.player2.shellTransform.position);
		this.frame = state.networkFrame;
	}
	#endregion

	#region IEquatable<FluxSimpleState> implementation
	public override bool Equals(object obj){
		return (obj is FluxSimpleState) && this.Equals((FluxSimpleState)obj);
	}

	public override int GetHashCode (){
		unchecked{
			return 
				11 * this.p1.GetHashCode() + 
				13 * this.p2.GetHashCode() + 
				17 * this.frame.GetHashCode();
		}
	}

	public bool Equals(FluxSimpleState other){
		return 
			this.p1.Equals(other.p1) && 
			this.p2.Equals(other.p2) &&
			this.frame.Equals(other.frame);
	}
	#endregion

	#region public instance methods
	public byte[] Serialize(){
		return FluxSimpleState.Serialize(this);
	}
	#endregion

	#region public override methods
	public override string ToString (){
		return string.Format(
			"[FluxSimpleState | p1 = {0} | p2 = {1} | frame = {2}]", 
			this.p1.ToString(), 
			this.p2.ToString(), 
			this.frame
		);
	}
	#endregion

	#region public class methods
	public static void AddToStream(BinaryWriter writer, FluxSimpleState gameState){
		if (UFE.config.networkOptions.desynchronizationRecovery){
			writer.Write(gameState.p1.life);
			writer.Write(gameState.p1.gauge);
			writer.Write(gameState.p1.position.x);
			writer.Write(gameState.p1.position.y);
			writer.Write(gameState.p1.position.z);

			writer.Write(gameState.p2.life);
			writer.Write(gameState.p2.gauge);
			writer.Write(gameState.p2.position.x);
			writer.Write(gameState.p2.position.y);
			writer.Write(gameState.p2.position.z);

			writer.Write(gameState.frame);
		}else{
			writer.Write(gameState.p1.life);
			writer.Write(gameState.p2.life);
			writer.Write(gameState.frame);
		}
	}


	public static FluxSimpleState Deserialize(byte[] bytes){
		using (MemoryStream stream = new MemoryStream(bytes)){
			using (BinaryReader reader = new BinaryReader(stream)){
				return FluxSimpleState.ReadFromStream(reader);
			}
		}
	}

	public static FluxSimpleState ReadFromStream(BinaryReader reader){
		if (UFE.config.networkOptions.desynchronizationRecovery){
			return new FluxSimpleState(
				new PlayerInformation(
					reader.ReadSingle(),
					reader.ReadSingle(),
					new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())
				)
				,
				new PlayerInformation(
					reader.ReadSingle(),
					reader.ReadSingle(),
					new Vector3(reader.ReadSingle(), reader.ReadSingle(), reader.ReadSingle())
				)
				,
				reader.ReadInt64()
			);
		}else{
			float p1Life = reader.ReadSingle();
			float p2Life = reader.ReadSingle();
			long frame = reader.ReadInt64();

            return new FluxSimpleState(
				new PlayerInformation(p1Life, 0f, Vector3.zero),
				new PlayerInformation(p2Life, 0f, Vector3.zero),
				frame
			);
		}
	}

	public static byte[] Serialize(FluxSimpleState gameState){
		using (MemoryStream stream = new MemoryStream()){
			using (BinaryWriter writer = new BinaryWriter(stream)){
				FluxSimpleState.AddToStream(writer, gameState);
				writer.Flush();
				return stream.ToArray();
			}
		}
	}
	#endregion
}

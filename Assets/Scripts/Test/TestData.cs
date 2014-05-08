using UnityEngine;
using System.Collections.Generic;
using Ecosim.SceneData;
using Ecosim;

public class TestData : MonoBehaviour {
	

	
	Dictionary<Coordinate, int> dict;
	
	// Use this for initialization
	void Start () {
		long m1 = System.GC.GetTotalMemory(true);
	
		dict = new Dictionary<Coordinate, int>(257);
		
		for (int i = 0; i < 5000; i++) {
			dict.Add(new Coordinate(i % 256, i / 256), i % 255 + 1);
		}
		
		long m2 = System.GC.GetTotalMemory(true);
		Debug.Log("Memory " + ((m2 - m1) / 1024));
	}
	
	// Update is called once per frame
	void Update () {
	
	}
}

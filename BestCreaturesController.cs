﻿using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

public class BestCreaturesController : MonoBehaviour {

	public ViewController viewController;

	/// <summary>
	/// The camera that follows the creatures in the main evolution "scene".
	/// </summary>
	public CameraFollowScript MainCamera;
	/// <summary>
	/// The camera used to follow the best creature of the selected generation.
	/// </summary>
	public CameraFollowScript BCCamera;

	public Canvas EvolutionCanvas;
	public Canvas BCCanvas;

	public GameObject BCThumbScreen;
	public InputField BCGenerationInput;

	public Transform floorHeight;
	public Vector3 dropHeight;

	/// <summary>
	/// The list of best creature brains (as chromosome strings). The index + 1 = generation Number.
	/// </summary>
	private List<string> BestCreatures;
	//private List<float> BestFitness;
	private List<CreatureStats> BestCreatureStats;

	private Creature creature;
	public Creature Creature {
		set { creature = value; }
	}

	private Creature currentBest;
	/// <summary>
	/// The generation of the currently showing 
	/// </summary>
	private int currentGeneration;

	//private bool autoplayEnabled = true;
	private float autoplayDuration = 10f;
	private Coroutine autoplayRoutine;

	public GameObject Obstacle {
		set {
			this.obstacle = value;
		}
	}
	private GameObject obstacle;

	private Evolution evolution;

	// Use this for initialization
	void Start () {

		evolution = GameObject.FindGameObjectWithTag("Evolution").GetComponent<Evolution>();
		
		BestCreatures = new List<string>();
		BestCreatureStats = new List<CreatureStats>();

		autoplayDuration = evolution.Settings.simulationTime;
		viewController.autoplaySlider.value = autoplayDuration;

		BCThumbScreen.gameObject.SetActive(false);
	}
	
	// Update is called once per frame
	void Update () {

	}

	public void RefreshMuscleContractionVisibility() {
		if (currentBest != null) 
			currentBest.RefreshMuscleContractionVisibility(PlayerPrefs.GetInt(PlayerPrefsKeys.SHOW_MUSCLE_CONTRACTION, 0) == 1);
	}

	public void ShowBCThumbScreen() {
		BCThumbScreen.gameObject.SetActive(true);
	}

	/// <summary>
	/// Shows the best creatures "scene".
	/// </summary>
	public void ShowBestCreatures() {

		EvolutionCanvas.gameObject.SetActive(false);
		BCCanvas.gameObject.SetActive(true);

		MainCamera.SwitchToMiniViewport();
		BCCamera.SwitchToFullscreen();
	}

	/// <summary>
	/// Shows the main evolution / simulation scene.
	/// </summary>
	public void ShowEvolution() {

		EvolutionCanvas.gameObject.SetActive(true);
		BCCanvas.gameObject.SetActive(false);

		MainCamera.SwitchToFullscreen();
		BCCamera.SwitchToMiniViewport();
	}
		
	public void AddBestCreature(int generation, string chromosome, CreatureStats stats) {
		
		if (generation <= 0) throw new UnityException();

		BCThumbScreen.gameObject.SetActive(true);

		BestCreatures.Add(chromosome);

		BestCreatureStats.Add(stats);

		if (currentBest == null) {
			ShowBestCreature(1);
		}
	}

	/// <summary>
	/// Only call this when loading a saved evolution simulation.
	/// </summary>
	public void RunBestCreatures(int generation) {

		currentGeneration = generation;

		ShowBestCreature(generation);
	} 

	/// <summary>
	/// This function is called when the user finished selecting a new generation to show. 
	/// </summary>
	/// <param name="value">Value.</param>
	public void GenerationSelected(string value) {
		
		int generation;  
		if (!int.TryParse(value, out generation)) {
			throw new System.FormatException("The generation number value could not be parsed to an int");
		}

		GenerationSelected(generation);
	}

	private void GenerationSelected(int generation) {

		if (generation <= 0) {
			viewController.UpdateBCGeneration(currentGeneration);
			return;
		}
		
		// check to see if the selected generation was already simulated. If not, show a message.
		if (!GenerationSimulated(generation)) {

			viewController.UpdateBCGeneration(currentGeneration);
			viewController.ShowErrorMessage(string.Format("Generation {0} has not been simulated yet.\n\nCurrently Simulated up to Generation {1}", generation, BestCreatures.Count));
			return;
		} 

		viewController.HideErrorMessage();

		ShowBestCreature(generation);
	}

	private bool GenerationSimulated(int generation) {
		return BestCreatures.Count >= generation;
	}

	private void ShowBestCreature(int generation) {
		
		var chromosome = BestCreatures[generation - 1];
		SpawnCreature(chromosome);
		AutoPlay();

		currentGeneration = generation;
		viewController.UpdateBCGeneration(generation);
		viewController.UpdateStats(this.currentBest, BestCreatureStats[generation - 1]);
	}

	private void SpawnCreature(string chromosome) {

		if (this.creature == null) return;

		if (currentBest != null) {
			Destroy(currentBest.gameObject);
		}

		var creature = evolution.CreateCreature();
		evolution.ApplyBrain(creature, chromosome);
	
		BCCamera.toFollow = creature;
		currentBest = creature;

		creature.SetOnBestCreatureLayer();

		// THIS IS NEEDED! DO NOT REMOVE! IMPORTANT!
		creature.Alive = false;
		creature.gameObject.SetActive(false);

		creature.Alive = true;
		creature.gameObject.SetActive(true);
	}

	private Creature CreateCreature(){

		Creature creat = (Creature) ((GameObject) Instantiate(creature.gameObject, dropHeight, Quaternion.identity)).GetComponent<Creature>();
		creat.RefreshLineRenderers();
		return creat;
	}

	public void GoToNextGeneration() {
		
		GenerationSelected(currentGeneration + 1);
	}

	public void GoToPreviousGeneration() {
		
		GenerationSelected(currentGeneration - 1);
	}

	private void AutoPlay() {

		if (!viewController.shouldAutoplay) {
		
			StopAutoPlay();
			return;
		}

		StopAutoPlay();
		autoplayRoutine = StartCoroutine(ShowNextAfterTime(autoplayDuration));
	}

	private IEnumerator ShowNextAfterTime(float time) {
		yield return new WaitForSeconds(time);

		// Check to see if the next generation has been simulated yet,
		// otherwise wait for 1 / 3 of time again.
		while (!GenerationSimulated(currentGeneration + 1)) {
			yield return new WaitForSeconds(time / 3);
		}

		GoToNextGeneration();
	}

	private void StopAutoPlay() {
		
		if (autoplayRoutine != null) StopCoroutine(autoplayRoutine);
	}

	public void AutoPlaySwitched(bool value) {

		//autoplayEnabled = value;
		if (value) {
			AutoPlay();
		} else {
			StopAutoPlay();
		}

		viewController.ViewAutoPlaySettings(value);
	}

	public void AutoPlayDurationChanged(float value) {
		autoplayDuration = value;
		viewController.UpdateAutoPlayDurationLabel(value);
	}

	public void SetBestChromosomes(List<ChromosomeStats> bestChroms) {

		BestCreatures.Clear();
		BestCreatureStats.Clear();

		foreach (var chromosomeInfo in bestChroms) {

			BestCreatures.Add(chromosomeInfo.chromosome);
			BestCreatureStats.Add(chromosomeInfo.stats);
		}
	}

	public List<ChromosomeStats> GetBestChromosomes() {

		var bestChroms = new List<ChromosomeStats>();

		for (int i = 0; i < BestCreatures.Count; i++) {
			bestChroms.Add(new ChromosomeStats(BestCreatures[i], BestCreatureStats[i]));
		}

		return bestChroms;
	}
}

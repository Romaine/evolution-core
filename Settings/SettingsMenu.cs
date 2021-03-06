﻿using System.Collections;
using System.Collections.Generic;
using System;
using UnityEngine;
using UnityEngine.UI;

public class SettingsMenu : MonoBehaviour {

	public const string GRID_SIZE_KEY = "GRID_SIZE";
	public const string GRID_ENABLED_KEY = "GRID_ENABLED";

	private const string EVOLUTION_SETTINGS_KEY = "EVOLUTION_SETTINGS";

	private static string[] TASK_OPTIONS = new string[] {"RUNNING", "JUMPING", "OBSTACLE JUMP", "CLIMBING"};

	private const float DEFAULT_GRID_SIZE = 2.0f;

	[SerializeField] private AutoScroll contentContainer;

	// Grid stuff
	[SerializeField] private Grid grid;
	[SerializeField] private Slider gridSizeSlider;
	[SerializeField] private Text gridSizeLabel;
	[SerializeField] private Toggle gridToggle;

	// Keep best creatures
	[SerializeField] private Toggle keepBestCreaturesToggle;

	// Batch simulation
	[SerializeField] private InputField batchSizeInput;
	[SerializeField] private Toggle batchSizeToggle;

	// Population size
	[SerializeField] private InputField populationSizeInput;

	// Simulation Time
	[SerializeField] private InputField simulationTimeInput;

	// Evolution Task
	[SerializeField] private Dropdown taskDropdown;

	// Mutation rate
	[SerializeField] private InputField mutationRateInput;

	// Use this for initialization
	void Start () {
		Setup();
	}

	private void Setup() {

		SetupInputFieldCallbacks();
		SetupTaskDropDown();

		// Setup the grid stuff
		var gridSize = PlayerPrefs.GetFloat(GRID_SIZE_KEY, DEFAULT_GRID_SIZE);
		grid.Size = gridSize;
		gridSizeSlider.value = gridSize;
		UpdateGridSizeText(gridSize);

		var gridEnabled = PlayerPrefs.GetInt(GRID_ENABLED_KEY, 0) == 1;
		grid.gameObject.SetActive(gridEnabled);
		gridToggle.isOn = gridEnabled;

		// Evolution settings
		var settings = LoadEvolutionSettings();

		keepBestCreaturesToggle.isOn = settings.keepBestCreatures;

		BatchSizeToggled(settings.simulateInBatches);
		batchSizeToggle.isOn = settings.simulateInBatches;

		batchSizeInput.text = settings.batchSize.ToString();
		populationSizeInput.text = settings.populationSize.ToString();
		simulationTimeInput.text = settings.simulationTime.ToString();
		mutationRateInput.text = settings.mutationRate.ToString();

		/*var keepBestCreatures = PlayerPrefs.GetInt(KEEP_BEST_CREATURE_KEY, 0) == 1;
		keepBestCreaturesToggle.isOn = keepBestCreatures;

		var batchSimulationEnabled = PlayerPrefs.GetInt(BATCH_SIMULATION_ENABLED_KEY, 0) == 1;
		BatchSizeToggled(batchSimulationEnabled);
		batchSizeToggle.isOn = batchSimulationEnabled;*/
	}

	private void SetupInputFieldCallbacks() {

		populationSizeInput.onEndEdit.AddListener(delegate {
			PopulationSizeChanged();
		});

		simulationTimeInput.onEndEdit.AddListener(delegate {
			SimulationTimeChanged();
		});

		batchSizeInput.onEndEdit.AddListener(delegate {
			BatchSizeChanged();
		});

		mutationRateInput.onEndEdit.AddListener(delegate {
			MutationRateChanged();
		});
	}

	private void SetupTaskDropDown() {

		var settings = LoadEvolutionSettings();
		var taskString = settings.task.StringRepresentation().ToUpper();

		var index = new List<string>(TASK_OPTIONS).IndexOf(taskString);

		taskDropdown.value = index;
	}

	public void TaskChanged() {

		var settings = LoadEvolutionSettings();
		settings.task = EvolutionTaskUtil.TaskFromString(taskDropdown.captionText.text);
		SaveEvolutionSettings(settings);
	}

	public void Show() {
		contentContainer.gameObject.SetActive(true);
	}

	public void Hide() {
		contentContainer.gameObject.SetActive(false);
	}

	public void GridToggled(bool val) {

		PlayerPrefs.SetInt(GRID_ENABLED_KEY, val ? 1 : 0);

		grid.gameObject.SetActive(val);
	}

	public void GridSizeChanged(float value) {

		UpdateGridSizeText(value);
		grid.Size = value;
		grid.VisualRefresh();

		PlayerPrefs.SetFloat(GRID_SIZE_KEY, value);
	}

	private void UpdateGridSizeText(float value) {
		gridSizeLabel.text = value.ToString("0.0");
	}

	public void KeepBestCreaturesToggled(bool value) {

		var settings = LoadEvolutionSettings();
		settings.keepBestCreatures = value;
		SaveEvolutionSettings(settings);
	}

	private void PopulationSizeChanged() {
		// Make sure the size is at least 2
		var num = Mathf.Clamp(Int32.Parse(populationSizeInput.text), 2, 10000000);
		populationSizeInput.text = num.ToString();

		var settings = LoadEvolutionSettings();
		settings.populationSize = num;
		SaveEvolutionSettings(settings);
	}

	private void SimulationTimeChanged() {
		// Make sure the time is at least 1
		var time = Mathf.Clamp(Int32.Parse(simulationTimeInput.text), 1, 100000);
		simulationTimeInput.text = time.ToString();

		var settings = LoadEvolutionSettings();
		settings.simulationTime = time;
		SaveEvolutionSettings(settings);
	}

	private void MutationRateChanged() {
		// Clamp between 1 and 100 %
		var rate = Mathf.Clamp(int.Parse(mutationRateInput.text), 1, 100);
		mutationRateInput.text = rate.ToString();

		var settings = LoadEvolutionSettings();
		settings.mutationRate = rate;
		SaveEvolutionSettings(settings);
	}

	private void BatchSizeChanged() {
		// Make sure the size is between 1 and the population size
		var batchSize = ClampBatchSize(Int32.Parse(batchSizeInput.text));
		batchSizeInput.text = batchSize.ToString();

		var settings = LoadEvolutionSettings();
		settings.batchSize = batchSize;
		SaveEvolutionSettings(settings);
	}

	private int ClampBatchSize(int size) {

		var settings = LoadEvolutionSettings();
		var populationSize = settings.populationSize;

		return Mathf.Clamp(size, 1, populationSize);
	}

	public void BatchSizeToggled(bool val) {

		batchSizeInput.gameObject.SetActive(val);

		var settings = LoadEvolutionSettings();
		settings.simulateInBatches = val;
		SaveEvolutionSettings(settings);
	}

	/// <summary>
	/// Returns the chosen task based on the value of the taskDropDown;
	/// </summary>
	public EvolutionTask GetTask() {

		var taskString = taskDropdown.captionText.text.ToUpper();
		var task = EvolutionTaskUtil.TaskFromString(taskString);
		
		var settings = LoadEvolutionSettings();
		settings.task = task;
		SaveEvolutionSettings(settings);

		return task;
	}

	public EvolutionSettings GetEvolutionSettings() {
		return LoadEvolutionSettings();
	}

	private void SaveEvolutionSettings(EvolutionSettings settings) {
		PlayerPrefs.SetString(EVOLUTION_SETTINGS_KEY, settings.Encode());
	}

	/// <summary>
	/// Loads the currently stored Evolution settings.
	/// </summary>
	/// <returns>The evolution settings.</returns>
	private EvolutionSettings LoadEvolutionSettings() {

		var settingsString = PlayerPrefs.GetString(EVOLUTION_SETTINGS_KEY, "");

		if (settingsString == "") {
			// Default settings
			return new EvolutionSettings();
		}

		return EvolutionSettings.Decode(settingsString);
	}

	public NeuralNetworkSettings GetNeuralNetworkSettings() {
		return NeuralNetworkSettingsManager.GetNetworkSettings();
	}
}

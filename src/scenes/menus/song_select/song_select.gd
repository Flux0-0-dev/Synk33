extends Control


enum Difficulty {
	Easy,
	Normal,
	Hard,
	Expert
}


@export var songs: Array[Song] 


var current_difficulty: Difficulty:
	set(new):
		if current_difficulty == new:
			return
		current_difficulty = new
		filter_songs()
var current_song: Song:
	set = set_current_song


@onready var song_container: Container = %SongContainer
@onready var song_display_card: Container = %SongDisplayCard
@onready var song_cover: TextureRect = %SongCover
@onready var song_credit_bpm: Control = %BPMSongCredit
@onready var song_credit_length: Control = %LengthSongCredit
@onready var song_info_highscore: Control = %HighscoreSongInfo
@onready var song_credit_level: Control = %LevelSongCredit
@onready var song_credit_charter: Control = %CharterSongCredit
@onready var song_credit_genre: Control = %GenreSongCredit
@onready var song_name: Label = %SongName
@onready var song_credit: Label = %SongCredit

@onready var difficulty_select: DifficultySelect = %DifficultySelect
@onready var audio_stream_player: AudioStreamPlayer = $AudioStreamPlayer


static func time_as_string(time:float) -> String:
	return "%02d:%02d" % [time / 60, fmod(time, 60)]


func _ready() -> void:
	for song in songs:
		var song_button:SongButton = preload("res://scenes/menus/song_select/song_button.tscn").instantiate()
		song_button.icon = song.Cover
		song_button.title = song.Name
		song_button.credit = song.Author
		song_button.focus_entered.connect(set_current_song.bind(song))
		song_container.add_child(song_button)
	filter_songs()


func set_current_song(to:Song) -> void:
	if current_song == to:
		return
	current_song = to
	
	song_cover.texture = to.Cover
	song_credit_genre.info = to.Genre.to_upper()
	song_name.text = to.Name
	song_credit.text = to.Author
	audio_stream_player.stream = to.Audio
	audio_stream_player.play()
	
	var chart:Chart = to.GetChartByDifficulty(current_difficulty)
	if chart == null:
		(song_display_card.material as ShaderMaterial).set_shader_parameter(&"greyscale", true)
		audio_stream_player.stop()
		#assert(false, "No chart found for current difficulty")
		return
	for difficulty in len(Difficulty):
		(song_display_card.material as ShaderMaterial).set_shader_parameter(&"greyscale", false)
		difficulty_select.set_tab_availability(
			difficulty, 
			to.HasChart(difficulty)
		)
	change_chart(chart)


func change_chart(to:Chart) -> void:
	song_credit_bpm.info = "%03dBPM" % to.Song.Bpm
	song_credit_length.info = time_as_string(to.Song.Audio.get_length())
	
	song_info_highscore.info = str(SaveManager.GetChartHighScore(to.GetSaveHash()))
	
	song_credit_charter.info = to.Designer.to_upper()
	song_credit_level.info = str(to.Level)


func filter_songs() -> void:
	# This is weird and I don't like that we aren't looping
	# over the actual song buttons
	for i in len(songs): 
		var song:Song = songs[i]
		if not song.HasChart(current_difficulty):
			(song_container.get_child(i) as Control).change_disabled(true)
			continue
		var song_button: SongButton = (song_container.get_child(i) as Control)
		song_button.change_disabled(false)
		var chart = song.GetChartByDifficulty(current_difficulty)
		var performance = SaveManager.GetChartPerformance(chart.GetSaveHash())
		if performance != null:
			var rank = performance.GetRank()
			song_button.grade = rank
		else:
			song_button.grade = SongButton.Grade.NONE


func _on_difficulty_select_tab_changed(tab: int) -> void:
	current_difficulty = tab as Difficulty
	if not current_song:
		return
	if current_song.HasChart(tab as Difficulty):
		change_chart(current_song.GetChartByDifficulty(tab as Difficulty))
	else:
		push_warning("Current song does not have a chart for this difficulty")


func _on_play_pressed() -> void:
	if not current_song.HasChart(current_difficulty):
		push_warning("Current song does not have a chart for the selected difficulty, aborting")
		return
	TransitionManager.play_transition()
	await TransitionManager.transition_midpoint
	SceneManager.ChangeSceneToGame(current_song.GetChartByDifficulty(current_difficulty))

using UnityEngine;
using System.Collections;


public class MainMenu : MonoBehaviour 
{
	public enum TUTORIALS 
	{
		SIMPLE_TUTORIALS = 0, 
		HELLO_WORLD = 1, 
		CONTENT_TYPES = 2, 
		TRACKING_SAMPLES = 3, 
		DYNAMIC_MODELS = 4, 
		LOCATION_BASED_AR = 5, 
        QR_CODE_READER = 6,
		ADVANCED_TUTORIALS = 7, 
		INSTANT_TRACKING = 8, 
		EDGE_BASED_INITIALIZATION = 9,
		FACE_TRACKING = 10,
		INTERACTIVE_FURNITURE = 11,
		VISUAL_SEARCH = 12,
		ADVANCED_UNITY_TUTORIALS = 13,
		UNITY_PHYSICS = 14
	};

    private TUTORIALS currentTutorial = TUTORIALS.HELLO_WORLD;
	
	private int numberOfTutorials = 0;
	
	private enum STATES {LISTVIEW, TUTORIAL};
	private STATES state = STATES.LISTVIEW;
	
	public GUIStyle TutorialHelloWorldPreview;
	public GUIStyle TutorialContentTypesPreview;
	public GUIStyle TutorialTrackingSamplesPreview;
	public GUIStyle TutorialDynamicModelsPreview;
	public GUIStyle TutorialLocationBasedARPreview;
	public GUIStyle TutorialQRCodeReaderPreview;
	public GUIStyle TutorialInstantTrackingPreview;
	public GUIStyle TutorialEdgeBasedInitializationPreview;
	public GUIStyle TutorialFaceTrackingPreview;
	public GUIStyle TutorialInteractiveFurniturePreview;
	public GUIStyle TutorialVisualSearchPreview;
    public GUIStyle TutorialUnityPhysicsPreview;
	
	public GUIStyle bigHeadlineStyle;
	public GUIStyle headlineTextStyle;
	public GUIStyle descriptionTextStyle;
	public GUIStyle learnDescriptionTextStyle;
	public GUIStyle buttonTextStyle;
	public GUIStyle buttonStyle;
	public GUIStyle smallButtonStyle;
	
	private float scrollPosition = 0;
	
	private float maxScrollPosition = 0;
	private float minScrollPosition = 0;
	private float scrollHeight = 0;
	private float deltaY = 0;
	private float scrollSpeed = 250;
	private bool isScrolling = false;
	
	private float SizeFactor;
	
	private ScrollViewItem[] scrollViewItems;
	
	// Use this for initialization
	void Start () 
	{
		numberOfTutorials = System.Enum.GetValues(typeof(TUTORIALS)).Length;
		
		scrollViewItems = new ScrollViewItem[numberOfTutorials];
		
		SizeFactor = GUIUtilities.SizeFactor;
		
		foreach(int tut in System.Enum.GetValues(typeof(TUTORIALS)))
		{
			if(tut == (int)TUTORIALS.SIMPLE_TUTORIALS)
			{
				scrollViewItems[tut] = new HeadlineScrollItem("Basic Tutorials", bigHeadlineStyle);	
			}
			else if(tut == (int)TUTORIALS.ADVANCED_TUTORIALS)
			{
				scrollViewItems[tut] = new HeadlineScrollItem("Advanced Tutorials", bigHeadlineStyle);
			}
			else if(tut == (int)TUTORIALS.ADVANCED_UNITY_TUTORIALS)
			{
				scrollViewItems[tut] = new HeadlineScrollItem("Advanced Unity Examples", bigHeadlineStyle);
			}
			else
			{
				scrollViewItems[tut] = new TutorialScrollItem(
					getTutorialIcon((TUTORIALS)tut),
					headlineTextStyle,
					buttonStyle,
					getTutorialName((TUTORIALS)tut),
					(TUTORIALS)tut,
					this);
			}
			minScrollPosition -= scrollViewItems[tut].getHeight();
			scrollHeight += scrollViewItems[tut].getHeight();
		}
		
		minScrollPosition += Screen.height;
		
		if(PlayerPrefs.HasKey("currentTutorial") && PlayerPrefs.HasKey("backFromARScene"))
		{
			if(PlayerPrefs.GetInt("backFromARScene") == 1)
				goToTutorial((TUTORIALS)PlayerPrefs.GetInt("currentTutorial"));
			
			PlayerPrefs.DeleteKey("currentTutorial");
			PlayerPrefs.DeleteKey("backFromARScene");
		}
	}
	
	private void calcSizes () 
	{
		maxScrollPosition = 0;
		minScrollPosition = 0;
		scrollHeight = 0;
			
		foreach(int tut in System.Enum.GetValues(typeof(TUTORIALS)))
		{
			minScrollPosition -= scrollViewItems[tut].getHeight();
			scrollHeight += scrollViewItems[tut].getHeight();
		}
		
		minScrollPosition += Screen.height;
	}
	
	// Update is called once per frame
	void Update () 
	{	
		if (Input.GetKeyDown(KeyCode.Escape))
		{
			if(state == STATES.LISTVIEW)
				Application.Quit();
			else
				state = STATES.LISTVIEW;
		}
		
		
		SizeFactor = GUIUtilities.SizeFactor;
		
		calcSizes();
		
		if(state == STATES.LISTVIEW || state == STATES.TUTORIAL)
		{
			if (Input.GetAxis("Mouse ScrollWheel") != 0)
			{
				float delta = Input.GetAxis("Mouse ScrollWheel") * scrollSpeed;
				scrollPosition = Mathf.Clamp((scrollPosition + delta), minScrollPosition, maxScrollPosition);
			}
	
			if (Input.touchCount > 0) 
			{	
				Touch touch = Input.GetTouch (0);
	         	if (touch.phase == TouchPhase.Moved && touch.deltaTime > 0) 
				{
					deltaY = (touch.deltaPosition * (Time.deltaTime / touch.deltaTime)).y;
					scrollPosition = Mathf.Clamp ((scrollPosition - deltaY), minScrollPosition, maxScrollPosition);
					isScrolling = true;
	         	}
			}
			
			if ((Input.touchCount > 0 && Input.GetTouch(0).phase == TouchPhase.Ended))
				isScrolling = false;
			
		}
		
	}
	
	
	void OnGUI () 
	{	
		switch(state)
		{
			case STATES.LISTVIEW:
				renderListview();
				break;
				
			case STATES.TUTORIAL:
				renderTutorialview();
				break;
		}
	}
	
	private void renderTutorialview(){
		
		if(GUIUtilities.ButtonWithText(new Rect(
			40*SizeFactor,
			20*SizeFactor,
			100*SizeFactor,
			50*SizeFactor),"Back",smallButtonStyle,buttonTextStyle)) {
				state = STATES.LISTVIEW;
			}
		
		if(GUIUtilities.ButtonWithText(new Rect(
			300*SizeFactor + GUIUtilities.getSize(headlineTextStyle,new GUIContent(getTutorialName(currentTutorial))).x / 4,
			(150+20)*SizeFactor + GUIUtilities.getSize(headlineTextStyle,new GUIContent(getTutorialName(currentTutorial))).y*2,
			100*SizeFactor,
			50*SizeFactor),"Start",smallButtonStyle,buttonTextStyle)) {
			
#if UNITY_EDITOR || UNITY_STANDALONE_OSX || UNITY_STANDALONE_WIN
				if(currentTutorial==TUTORIALS.VISUAL_SEARCH ||
				   currentTutorial==TUTORIALS.INTERACTIVE_FURNITURE ||
				   currentTutorial==TUTORIALS.LOCATION_BASED_AR)
				{
					Debug.LogError("This tutorial is currently not implemented for the Editor Preview and Windows/Mac Standalone, please try on a mobile device");
					return;
				}
#endif
				PlayerPrefs.SetInt("currentTutorial", (int)currentTutorial);
				Application.LoadLevel(getSceneName(currentTutorial));
			}
		
			
		GUI.Label(new Rect(
			40*SizeFactor,
			90*SizeFactor,
			220*SizeFactor,
			220*SizeFactor),"",getTutorialIcon(currentTutorial));
		
		GUIUtilities.Text(new Rect(
			300*SizeFactor,
			100*SizeFactor,  
			Screen.width - (300*SizeFactor), 
			GUIUtilities.getSize(headlineTextStyle,new GUIContent(getTutorialName(currentTutorial))).y)
			,getTutorialName(currentTutorial), headlineTextStyle);
		
		GUIUtilities.Text(new Rect(
			40*SizeFactor,
			350*SizeFactor,  
			Screen.width - (40*SizeFactor), 
			GUIUtilities.getSize(headlineTextStyle,new GUIContent(getTutorialName(currentTutorial))).y)
			,"Learn", headlineTextStyle);
		
		GUIUtilities.Text(new Rect(
			40*SizeFactor,
			350*SizeFactor + GUIUtilities.getSize(headlineTextStyle,new GUIContent(getTutorialName(currentTutorial))).y,  
			Screen.width - (40*SizeFactor), 
			GUIUtilities.getSize(descriptionTextStyle,new GUIContent(getTutorialName(currentTutorial))).y)
			,getLearn(currentTutorial), learnDescriptionTextStyle);
		
		GUIUtilities.Text(new Rect(
			40*SizeFactor,
			380*SizeFactor + GUIUtilities.getSize(headlineTextStyle,new GUIContent(getTutorialName(currentTutorial))).y * 4,  
			Screen.width - (40*SizeFactor), 
			GUIUtilities.getSize(headlineTextStyle,new GUIContent(getTutorialName(currentTutorial))).y)
			,"Key methods", headlineTextStyle);
		
		GUIUtilities.Text(new Rect(
			40*SizeFactor,
			380*SizeFactor + GUIUtilities.getSize(headlineTextStyle,new GUIContent(getTutorialName(currentTutorial))).y * 5,  
			Screen.width - (40*SizeFactor), 
			GUIUtilities.getSize(learnDescriptionTextStyle,new GUIContent(getTutorialName(currentTutorial))).y)
			,getKeyMethods(currentTutorial), learnDescriptionTextStyle);
	}
	
	private void renderListview()
	{
		GUI.BeginGroup (new Rect (0,scrollPosition, Screen.width, scrollHeight));
		
		int index = 0;
		int height = 0;
		
		foreach(int tut in System.Enum.GetValues(typeof(TUTORIALS)))
		{
			
			GUI.BeginGroup(new Rect(0, height, Screen.width, scrollViewItems[tut].getHeight()));
			scrollViewItems[tut].renderMe(isScrolling);
			GUI.EndGroup();
			
			height += scrollViewItems[tut].getHeight();
			index++;
		}
		
		GUI.EndGroup();	
	}
	
	public void goToTutorial(TUTORIALS tutorial)
	{
		state = STATES.TUTORIAL;
		currentTutorial = tutorial;
	}
			
	private string getTutorialName(TUTORIALS tutorial)
	{
		switch (tutorial) {
			
		case TUTORIALS.HELLO_WORLD:
			return "Hello World!";
		case TUTORIALS.CONTENT_TYPES:
			return "Content Types";
		case TUTORIALS.TRACKING_SAMPLES:
			return "Tracking Samples";
		case TUTORIALS.DYNAMIC_MODELS:
			return "Dynamic Models";
		case TUTORIALS.LOCATION_BASED_AR:
			return "Location-based AR";
		case TUTORIALS.INSTANT_TRACKING:
			return "Instant Tracking";
		case TUTORIALS.EDGE_BASED_INITIALIZATION: 
			return "Edge Based Initialization";
		case TUTORIALS.FACE_TRACKING: 
			return "Face Tracking (beta)";
		case TUTORIALS.INTERACTIVE_FURNITURE:
			return "Interactive Furniture";
		case TUTORIALS.VISUAL_SEARCH:
			return "Visual Search";
		case TUTORIALS.UNITY_PHYSICS: 
			return "Unity Physics";
        case TUTORIALS.QR_CODE_READER:
            return "QR Code Reader";
		}
		
		return "name not defined";
	}
	
	private string getLearn(TUTORIALS tutorial)
	{
		switch (tutorial) {
			
		case TUTORIALS.HELLO_WORLD:
			return "How to place 3D model to the scene and apply model transformations.";
		case TUTORIALS.CONTENT_TYPES:
			return "How to place image and video as a content. Load environment map to a 3D model. Controlling playback of the movie.";
		case TUTORIALS.TRACKING_SAMPLES:
			return "How to use ID and picture marker, markerless tracking technique. Assign multiple tracking references in one configuration file.";
		case TUTORIALS.DYNAMIC_MODELS:
			return "Starting model animation. Handling touch events. Getting tracking values of the scene.";
		case TUTORIALS.LOCATION_BASED_AR:
			return "Load POI to the scene. Create custom graphics to billboards. How to use non-optical tracking technology. Add radar into your scene.";
		case TUTORIALS.INSTANT_TRACKING:
			return "Instant 2D and 3D (SLAM) tracking techniques. Rectify a camera image so that an object is displayed correctly on a flat surface. For instant 3D tracking we recommend to move around the scene like on the picture below to create a map.";
		case TUTORIALS.EDGE_BASED_INITIALIZATION:
			return "Use a 3D model for initial camera registration and continue with Markerless 3D tracking in a real world scale. Loading and configuring models for Edge Based Initialization. For indoor or outdoor applications and with fixed or freely navigable view";
		case TUTORIALS.FACE_TRACKING:
			return "How to perform face tracking (beta)";
		case TUTORIALS.INTERACTIVE_FURNITURE:
			return "Currently unavailable.";
		case TUTORIALS.VISUAL_SEARCH:
			return "Request a visual search. Register visual search callback to monitor the status of visual search and perform tracking if successful.";
		case TUTORIALS.UNITY_PHYSICS:
			return "Learn how to use Unity's physics engine.";
        case TUTORIALS.QR_CODE_READER:
            return "How to implement QR Code reader";
		}
		
		return "name not defined";
	}
	
	private string getKeyMethods(TUTORIALS tutorial)
	{
		switch (tutorial) {
			
		case TUTORIALS.HELLO_WORLD:
			return "";
		case TUTORIALS.CONTENT_TYPES:
			return "startMovieTexture()\nIMetaioSDK::loadEnvironmentMap()";
		case TUTORIALS.TRACKING_SAMPLES:
			return "IMetaioSDK::setTrackingConfiguration()\nIMetaioSDK::setCoordinateSystemID()";
		case TUTORIALS.DYNAMIC_MODELS:
			return "IMetaioSDK::getTrackingValues()";
		case TUTORIALS.LOCATION_BASED_AR:
			return "setTranslationLLA()";
		case TUTORIALS.INSTANT_TRACKING:
			return "IMetaioSDK::startInstantTracking()\nonInstantTrackingEvent()";
		case TUTORIALS.EDGE_BASED_INITIALIZATION:
			return "IMetaioSDK::setTrackingConfiguration()\nIMetaioSDK::sensorCommand()";
		case TUTORIALS.FACE_TRACKING:
			return "IMetaioSDK::setTrackingConfiguration";
		case TUTORIALS.INTERACTIVE_FURNITURE:
			return "";
		case TUTORIALS.VISUAL_SEARCH:
			return "requestVisualSearch()\nregisterVisualSearchCallback()\nonVisualSearchResult()\nonVisualSearchStatusChanged()";
		case TUTORIALS.UNITY_PHYSICS:
			return "Learn how to use Unity's physics engine.";
        case TUTORIALS.QR_CODE_READER:
            return "IMetaioSDK::setTrackingConfiguration()\nIMetaioSDKCallback::onTrackingEvent()\nTrackingValues::getAdditionalValues()";
		}
		
		return "name not defined";
	}	

	
	private GUIStyle getTutorialIcon(TUTORIALS tutorial)
	{
		switch (tutorial) {
			
		case TUTORIALS.HELLO_WORLD:
			return TutorialHelloWorldPreview;
		case TUTORIALS.CONTENT_TYPES:
			return TutorialContentTypesPreview;
		case TUTORIALS.TRACKING_SAMPLES:
			return TutorialTrackingSamplesPreview;
        case TUTORIALS.DYNAMIC_MODELS:
			return TutorialDynamicModelsPreview;
		case TUTORIALS.LOCATION_BASED_AR:
			return TutorialLocationBasedARPreview;
		case TUTORIALS.INSTANT_TRACKING:
			return TutorialInstantTrackingPreview;
		case TUTORIALS.EDGE_BASED_INITIALIZATION:
			return TutorialEdgeBasedInitializationPreview;
		case TUTORIALS.FACE_TRACKING:
			return TutorialFaceTrackingPreview;
		case TUTORIALS.INTERACTIVE_FURNITURE:
			return TutorialInteractiveFurniturePreview;
		case TUTORIALS.VISUAL_SEARCH:
			return TutorialVisualSearchPreview;
		case TUTORIALS.UNITY_PHYSICS:
			return TutorialUnityPhysicsPreview;
        case TUTORIALS.QR_CODE_READER:
            return TutorialQRCodeReaderPreview;
		}
		
		return TutorialHelloWorldPreview;
	}

	private string getSceneName(TUTORIALS tutorial)
	{
		switch (tutorial) {
			
		case TUTORIALS.HELLO_WORLD:
			return "HelloWorld";
		case TUTORIALS.CONTENT_TYPES:
			return "ContentTypes";
		case TUTORIALS.TRACKING_SAMPLES:
			return "TrackingSamples";
        case TUTORIALS.DYNAMIC_MODELS:
			return "DynamicModels";
		case TUTORIALS.LOCATION_BASED_AR:
			return "LocationBasedAR";
		case TUTORIALS.INSTANT_TRACKING:
			return "InstantTracking";
		case TUTORIALS.EDGE_BASED_INITIALIZATION:
			return "EdgeBasedInitialization";
		case TUTORIALS.FACE_TRACKING:
			return "FaceTracking";
		case TUTORIALS.INTERACTIVE_FURNITURE:
			return "InteractiveFurniture";
		case TUTORIALS.VISUAL_SEARCH:
			return "VisualSearch";
		case TUTORIALS.UNITY_PHYSICS:
			return "UnityPhysics";
        case TUTORIALS.QR_CODE_READER:
            return "QRCodeReader";
		}
		
		return "";
	}
}

public abstract class ScrollViewItem
{
	public abstract void renderMe(bool isScrolling);
	public abstract int getHeight();
}

public class HeadlineScrollItem : ScrollViewItem
{
	private string headLine;
	private GUIStyle bigHeadlineStyle;
	
	public HeadlineScrollItem (string headLine, GUIStyle bigHeadlineStyle)
	{
		this.headLine = headLine;
		this.bigHeadlineStyle = bigHeadlineStyle;
	}
	
	public override void renderMe (bool isScrolling)
	{
		GUIUtilities.Text(new Rect(
			10*GUIUtilities.SizeFactor,
			30*GUIUtilities.SizeFactor,
			Screen.width,
			GUIUtilities.getSize(bigHeadlineStyle, new GUIContent(headLine)).y)
			, this.headLine, bigHeadlineStyle);
	}
	
	public override int getHeight ()
	{
		return (int)(GUIUtilities.getSize(bigHeadlineStyle, new GUIContent(headLine)).y + 80 * GUIUtilities.SizeFactor);
	}	
}

public class TutorialScrollItem : ScrollViewItem
{
	private GUIStyle icon;
	private GUIStyle headlineStyle;
	private GUIStyle buttonStyle;
	private string headline;
	private MainMenu.TUTORIALS tutorial;
	private MainMenu mainMenu;
	
	public TutorialScrollItem (GUIStyle icon, GUIStyle headlineStyle, GUIStyle buttonStyle, string headline, MainMenu.TUTORIALS tutorial, MainMenu mainMenu)
	{
		this.icon = icon;
		this.headlineStyle = headlineStyle;
		this.buttonStyle = buttonStyle;
		this.headline = headline;
		this.tutorial = tutorial;
		this.mainMenu = mainMenu;
	}
	
	public override void renderMe (bool isScrolling)
	{
		
		if(GUI.Button(new Rect(40*GUIUtilities.SizeFactor, 0, Screen.width - 80 * GUIUtilities.SizeFactor, 200 * GUIUtilities.SizeFactor), "", buttonStyle) && !isScrolling )
		{
			mainMenu.goToTutorial(tutorial);
		}
		
		GUI.Label(new Rect(
			80*GUIUtilities.SizeFactor,
			40*GUIUtilities.SizeFactor,
			140*GUIUtilities.SizeFactor,
			140*GUIUtilities.SizeFactor),"",icon);
		
		GUIUtilities.Text(new Rect(
			300*GUIUtilities.SizeFactor,
			35*GUIUtilities.SizeFactor,  
			Screen.width - (300*GUIUtilities.SizeFactor), 
			GUIUtilities.getSize(headlineStyle,new GUIContent(this.headline)).y),this.headline, this.headlineStyle);
		
	}

	public override int getHeight ()
	{
		return (int)(200 * GUIUtilities.SizeFactor);
	}
	
	
	
}
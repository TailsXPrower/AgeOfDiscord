namespace Sandbox.Modes;

public interface IMode
{
	public static IMode BuildMode = new BuildMode();
	public static IMode ProvinceMode = new ProvinceMode();
	public static IMode UnitMode = new UnitMode();
	public static IMode CultureMode = new CultureMode();

	public void OnDeselect( Scene scene );
	public void OnSelect( Scene scene );
	public void Process(Scene scene);
}

namespace IsoMauiEngine.Navigation;

public interface INavigator
{
	NavPath ComputePath(NavRequest request);
}

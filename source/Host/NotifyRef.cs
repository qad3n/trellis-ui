namespace Trellis
{
	// resolves an id a message mentions into the name it spells out and the name to show
	internal delegate bool NotifyRef(int id, out string plain, out string raw);
}

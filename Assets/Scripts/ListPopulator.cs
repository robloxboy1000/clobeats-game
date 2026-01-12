using UnityEngine;
using UnityEngine.UIElements;
using System.Collections.Generic;

public class ListPopulator : MonoBehaviour
{
    // The list of data you want to display
    List<string> itemList = new List<string> { "Item 1", "Item 2", "Item 3", "Item 4", "Item 5", "Item 6" };

    void OnEnable()
    {
        // Get the root visual element from the UIDocument component
        var uiDocument = GetComponent<UIDocument>();
        var root = uiDocument.rootVisualElement;

        // Query for the ListView by name from your UXML (assuming its name is "my-list-view")
        // If not using UXML, you can create it in code: var listView = new ListView(itemList);
        ListView listView = new ListView(itemList);

        // Set the source of the list
        listView.itemsSource = itemList;

        // Provide a function for creating a new visual element for each list item
        listView.makeItem = () => new Label();

        // Provide a function for binding the data to the visual element
        listView.bindItem = (element, index) =>
        {
            (element as Label).text = itemList[index];
        };

        // Set a fixed height for each item
        listView.fixedItemHeight = 20;

        // Ensure the list view is scrollable if items overflow
        listView.reorderMode = ListViewReorderMode.Animated; // Enables reordering but also helps with layout
        
        // Refresh the view to display items
        listView.RefreshItems();
    }
}


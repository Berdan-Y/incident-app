using System.Text.RegularExpressions;

namespace Maui.Behaviors;

public class EmailValidationBehavior : Behavior<Entry>
{
    private Entry _entry;
    private Label _errorLabel;
    private bool _hasBeenUnfocused;

    private static readonly Regex EmailRegex = new(
        @"^[a-zA-Z0-9._%+-]+@[a-zA-Z0-9.-]+\.[a-zA-Z]{2,}$",
        RegexOptions.Compiled | RegexOptions.IgnoreCase);

    protected override void OnAttachedTo(Entry entry)
    {
        _entry = entry;
        entry.Unfocused += OnEntryUnfocused;
        entry.TextChanged += OnEntryTextChanged;

        // Find the error label in the same parent layout
        _errorLabel = FindErrorLabel(entry);

        base.OnAttachedTo(entry);
    }

    protected override void OnDetachingFrom(Entry entry)
    {
        entry.Unfocused -= OnEntryUnfocused;
        entry.TextChanged -= OnEntryTextChanged;
        _entry = null;
        _errorLabel = null;
        base.OnDetachingFrom(entry);
    }

    private void OnEntryTextChanged(object sender, TextChangedEventArgs e)
    {
        if (_hasBeenUnfocused)
        {
            ValidateEmailField(e.NewTextValue);
        }
    }

    private void OnEntryUnfocused(object sender, FocusEventArgs e)
    {
        _hasBeenUnfocused = true;
        if (sender is Entry entry)
        {
            ValidateEmailField(entry.Text);
        }
    }

    private void ValidateEmailField(string email)
    {
        bool isValid = ValidateEmail(email);

        // Visual feedback on the entry
        if (_entry != null)
        {
            _entry.TextColor = string.IsNullOrEmpty(email) || isValid ? Colors.Black : Colors.Red;
        }

        // Show/hide error message
        if (_errorLabel != null)
        {
            if (!string.IsNullOrEmpty(email) && !isValid)
            {
                _errorLabel.Text = "Please enter a valid email address (example@domain.com)";
                _errorLabel.IsVisible = true;
            }
            else
            {
                _errorLabel.IsVisible = false;
            }
        }
    }

    private Label FindErrorLabel(Entry entry)
    {
        // The error label should be the next element after the entry in the layout
        if (entry.Parent is Layout layout)
        {
            var children = layout.Children.ToList();
            int entryIndex = children.IndexOf(entry);
            if (entryIndex >= 0 && entryIndex + 1 < children.Count)
            {
                var nextElement = children[entryIndex + 1];
                if (nextElement is Label label && label.StyleId == "EmailErrorLabel")
                {
                    return label;
                }
            }
        }
        return null;
    }

    public static bool ValidateEmail(string email)
    {
        if (string.IsNullOrWhiteSpace(email))
            return false;

        if (email.Length < 3)
            return false;

        return EmailRegex.IsMatch(email);
    }
}
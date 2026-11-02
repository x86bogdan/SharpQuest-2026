namespace SharpQuest.Contracts.Tests.Lab09;

/// <summary>
/// Lab 09: the buttons. A command is two questions the window keeps asking — can I? and, when
/// clicked, do it — plus an event that tells the button to ask again.
/// </summary>
public class CommandTests : Lab09TestBase
{
    [Fact]
    public void The_commands_are_the_same_objects_every_time()
    {
        var (_, vm) = Open(1);

        Assert.True(ReferenceEquals(vm.AddCommand, vm.AddCommand) && ReferenceEquals(vm.RemoveCommand, vm.RemoveCommand),
            "A command property builds a new object each time it is read. The button binds once, to the first " +
            "one, and never hears from any of the others — so CanExecuteChanged never reaches it.");
    }

    [Fact]
    public void Add_is_always_available()
    {
        var (_, vm) = Open(1);

        Assert.True(vm.AddCommand.CanExecute(Wiring.CreateSample(9)), "AddCommand says it can't add anything.");
    }

    [Fact]
    public void Add_puts_it_in_the_store()
    {
        var (store, vm) = Open(1, 2);

        vm.AddCommand.Execute(Wiring.CreateSample(3));

        Assert.True(store.Count == 3,
            $"After Add, the store holds {store.Count} items instead of 3. The command goes through the store — " +
            "that is what keeps Lab 02's rules working and what makes Lab 08 able to save it.");
    }

    [Fact]
    public void And_in_the_list()
    {
        var (_, vm) = Open(1, 2);

        vm.AddCommand.Execute(Wiring.CreateSample(3));

        Assert.True(Shown(vm).Count == 3, $"The store took it but the list shows {Shown(vm).Count}.");
    }

    [Fact]
    public void Remove_is_greyed_out_when_nothing_is_selected()
    {
        var (_, vm) = Open(1, 2);

        Assert.False(vm.RemoveCommand.CanExecute(null),
            "RemoveCommand says it can run with nothing selected. CanExecute is what greys the button out; " +
            "a Delete button that is always clickable is a bug report waiting to happen.");
    }

    [Fact]
    public void And_available_when_something_is()
    {
        var (store, vm) = Open(1, 2);

        vm.Selected = store.All()[0];

        Assert.True(vm.RemoveCommand.CanExecute(null), "Something is selected and RemoveCommand still says no.");
    }

    [Fact]
    public void The_button_is_told_to_ask_again()
    {
        var (store, vm) = Open(1, 2);
        var asked = 0;
        void Handler(object? sender, EventArgs e) => asked++;
        vm.RemoveCommand.CanExecuteChanged += Handler;

        try
        {
            vm.Selected = store.All()[0];
        }
        finally
        {
            vm.RemoveCommand.CanExecuteChanged -= Handler;
        }

        Assert.True(asked > 0,
            "The selection changed, so RemoveCommand's answer changed, but it never raised CanExecuteChanged. " +
            "The button only re-asks when it is told to, so it stays greyed out with a row selected.");
    }

    [Fact]
    public void Remove_takes_it_out_of_the_store()
    {
        var (store, vm) = Open(1, 2, 3);
        vm.Selected = store.All()[1];
        var doomed = vm.Selected!.Id;

        vm.RemoveCommand.Execute(null);

        Assert.True(store.FindById(doomed) is null,
            $"'{doomed}' is still in the store after Remove.");
    }

    [Fact]
    public void And_out_of_the_list()
    {
        var (store, vm) = Open(1, 2, 3);
        vm.Selected = store.All()[1];
        var doomed = vm.Selected!.Id;

        vm.RemoveCommand.Execute(null);

        Assert.DoesNotContain(doomed, Shown(vm).Select(i => i.Id));
    }

    [Fact]
    public void And_nothing_is_selected_afterwards()
    {
        var (store, vm) = Open(1, 2);
        vm.Selected = store.All()[0];

        vm.RemoveCommand.Execute(null);

        Assert.True(vm.Selected is null,
            "The selected item was deleted and Selected still points at it. The details panel is now describing " +
            "something that isn't there.");
    }

    [Fact]
    public void Remove_with_nothing_selected_does_nothing_quietly()
    {
        var (store, vm) = Open(1, 2);

        var ex = Record.Exception(() => vm.RemoveCommand.Execute(null));

        Assert.True(ex is null, $"Execute with nothing selected threw {ex?.GetType().Name}. CanExecute said no, " +
                                "but a command still has to survive being run — bindings are not careful.");
        Assert.Equal(2, store.Count);
    }
}

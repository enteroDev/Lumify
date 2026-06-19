# GENERAL

## Fixes

* [x] AccountModal: Remove Workspace entry in Sidebar of AccountModal (Not needed probably)
* [x] FriendsPanel: Check LiveSync when getting added by another user. Needs a reload currently
* [ ] App-Layouts: Remove AccountModalProvider from auth-layout. It wont be neccessary here.
* [x] Check what happens if UserA adds UserB -> While UserB has not accepted, UserB tries to add UserA aswell. -> What happens? Maybe fix this situation.

  * An 3 Stellen gesichert! DB unique-index -> userHigh-userLow (Ist das selbe wie: 5+3 oder 3+5).
  * Also wenns eine Anfrage gibt, gibts die bei beiden usern. Also race condition auch hier nicht möglich.
  * Frontend: Bei eingehender Anfrage wird der add-Button durch "annehmen/Ablehnen" ersetzt. Auch hier alles bestens.

## Implementations

* [x] FriendsPanel: Add notification count in TabButton of Friendspanel aswell.
* [x] FriendsPanel: If no related user yet, display some other user, so the field is not empty. Or add a placeholder with text "Search for a user"
* [x] Implement Test Project for unit tests. -> Implement tests

<br>...<br>

# AUTH

## Fixes

* [x] Hitting "Enter" in PasswordField should also activate "LoginFunction" (Same as hitting the Button)

## Implementations

* Keine

<br>...<br>

# DASHBOARD

## Fixes

* [ ] Add NoMemberInfo in WorkspaceModal if no member yet. (Red Line like in FileView -> If no content in a folder)

## Implementations

* [ ] In module recents: Make the groups expandable. (Initially is closed. Click expands) -> Heading with icon is trigger. Expandable content = Content (Liste der Recents in der Gruppe).

<br>...<br>

# SPACEHUB

## Fixes

* Keine

## Implementations

* Keine

<br>...<br>

# NOTES

## Fixes

* [x] Root should not have a chevron at all. Needs to be blended away.
* [x] Make the chevron gray if expanding is not possible (Example: Folder to expand has no content in it. Therefore nothing is seen and the chevron is useless.)
* [x] If the user first clicks a file or potentionally also when clicking a folder in the fileTree, and THEN, while it is marked, deletes this file, FilwView shows message: File couldnt be found. In this case we want to stay in the currently shown folder of the fileView.

## Implementations

* Keine

<br>...<br>

# TODOS

## Fixes

* Keine

## Implementations

* Keine

<br>...<br>

# EVENTS

## Fixes

* [x] Set Calendar to current month (Show current month when visiting the Events-page)

## Implementations

* [x] Add logic to button "New Event" in eventDetails-modal (EventModal)
* [x] Use alert-modal at deleting MultiDay-Events. Tell the user that he is deleting the whole series if accepting the alert.

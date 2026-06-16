# GENERAL

## Fixes
[x] AccountModal: Remove Workspace entry in Sidebar of AccountModal (Not needed probably)
[x] FriendsPanel: Check LiveSync when getting added by another user. Needs a reload currently
[ ] App-Layouts: Remove AccountModalProvider from auth-layout. It wont be neccessary here.
[x] Check what happens if UserA adds UserB -> While UserB has not accepted, UserB tries to add UserA aswell. -> What happens? Maybe fix this situation.
    * An 3 Stellen gesichert! DB unique-index -> userHigh-userLow (Ist das selbe wie: 5+3 oder 3+5).
    * Also wenns eine Anfrage gibt, gibts die bei beiden usern. Also race condition auch hier nicht möglich.
    * Frontend: Bei eingehender Anfrage wird der add-Button durch "annehmen/Ablehnen" ersetzt. Auch hier alles bestens.

## Implementations
[x] FriendsPanel: Add notification count in TabButton of Friendspanel aswell.
[x] FriendsPanel: If no related user yet, display some other user, so the field is not empty. Or add a placeholder with text "Search for a user"
[x] Implement Test Project for unit tests. -> Implement tests


# AUTH

## Fixes
[x] Hitting "Enter" in PasswordField should also activate "LoginFunction" (Same as hitting the Button)

## Implementations



# DASHBOARD

## Fixes

## Implementations



# SPACEHUB

## Fixes

## Implementations



# NOTES

## Fixes
[x] Root should not have a chevron at all. Needs to be blended away.
[x] Make the chevron gray if expanding is not possible (Example: Folder to expand has no content in it. Therefore nothing is seen and the chevron is useless.)

## Implementations



# TODOS

## Fixes

## Implementations




# EVENTS

## Fixes
[x] Set Calendar to current month (Show current month when visiting the Events-page)

## Implementations
[x] Add logic to button "New Event" in eventDetails-modal (EventModal)
[x] Use alert-modal at deleting MultiDay-Events. Tell the user that he is deleting the whole series if accepting the alert.
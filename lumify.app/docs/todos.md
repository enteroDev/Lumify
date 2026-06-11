# GENERAL

## Fixes
[ ] AccountModal: Remove Workspace entry in Sidebar of AccountModal (Not needed probably)
[ ] FriendsPanel: Check LiveSync when getting added by another user. Needs a reload currently
[ ] App-Layouts: Remove AccountModalProvider from auth-layout. It wont be neccessary here.

## Implementations
[x] FriendsPanel: Add notification count in TabButton of Friendspanel aswell.
[ ] FriendsPanel: If no related user yet, display some other user, so the field is not empty. Or add a placeholder with text "Search for a user"
[ ] Implement Test Project for unit tests. -> Implement tests


# AUTH

## Fixes

## Implementations



# DASHBOARD

## Fixes

## Implementations



# SPACEHUB

## Fixes

## Implementations



# NOTES

## Fixes
[ ] Root should not have a chevron at all. Needs to be blended away.
[ ] Make the chevron gray if expanding is not possible (Example: Folder to expand has no content in it. Therefore nothing is seen and the chevron is useless.)

## Implementations



# TODOS

## Fixes

## Implementations




# EVENTS

## Fixes
[ ] Set Calendar to current month (Show current month when visiting the Events-page)

## Implementations
[ ] Add logic to button "New Event" in eventDetails-modal (EventModal)
[ ] Use alert-modal at deleting MultiDay-Events. Tell the user that he is deleting the whole series if accepting the alert.
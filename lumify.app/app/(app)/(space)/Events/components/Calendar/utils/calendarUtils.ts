// -------------- //
// --- Helper --- //
// -------------- //
export function mondayIndex(date: Date) {
    return (date.getDay() + 6) % 7;
}

export function ensureTwoDigits(n: number) {
    return String(n).padStart(2, "0");
}

export function getDateString(date: Date) {
    const year = date.getFullYear();
    const month = ensureTwoDigits(date.getMonth() + 1);
    const day = ensureTwoDigits(date.getDate());

    return `${year}-${month}-${day}`;
}

export function getDaysInMonth(viewYear: number, viewMonth: number) {
    // English: Day 0 of next month = last day of current month
    return new Date(viewYear, viewMonth + 1, 0).getDate();
}

export function needsSixthRow(viewYear: number, viewMonth: number) {
    const firstOfMonth = new Date(viewYear, viewMonth, 1);
    const offset = mondayIndex(firstOfMonth);
    const daysInMonth = getDaysInMonth(viewYear, viewMonth);

    // English: Number of cells needed to place all days with offset (Mon-left grid)
    const neededCells = offset + daysInMonth;

    // English: 5 rows = 35 cells, 6 rows = 42 cells
    return neededCells > 35;
}

export function buildMonthDays(viewYear: number, viewMonth: number, rows: 5 | 6) {
    const firstOfMonth = new Date(viewYear, viewMonth, 1);
    const offset = mondayIndex(firstOfMonth);

    const cellCount = rows * 7;

    const days: {
        date: string;
        isCurrentMonth: boolean;
    }[] = [];

    for (let i = 0; i < cellCount; i++) {
        const d = new Date(viewYear, viewMonth, 1 - offset + i);

        days.push({
            date: getDateString(d),
            isCurrentMonth: d.getMonth() === viewMonth,
        });
    }

    return days;
}
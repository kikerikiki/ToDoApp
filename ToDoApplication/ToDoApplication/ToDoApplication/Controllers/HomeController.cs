using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading.Tasks;
using ToDoApplication.Models;

public class HomeController : Controller
{
    private readonly TodoAppContext _context;
    private readonly Calendar _calendar;

    public HomeController(TodoAppContext context)
    {
        _context = context;
        _calendar = CultureInfo.CurrentCulture.Calendar;
    }

    // GET: Home/Index
    public async Task<IActionResult> Index()
    {
        var currentDate = DateTime.Now;
        var currentTodos = await _context.Todos
            .Where(t => t.DueDate.HasValue &&
                        (t.DueDate.Value.Year > currentDate.Year ||
                        (t.DueDate.Value.Year == currentDate.Year && t.DueDate.Value.Month >= currentDate.Month)))
            .OrderBy(t => t.DueDate)
            .ToListAsync();

        var groupedTodos = GroupTodosByMonthAndWeek(currentTodos);
        ViewData["Title"] = "Todo-Liste";

        return View(groupedTodos);
    }

    [HttpPost]
    public async Task<IActionResult> ToggleCompletion(int id)
    {
        var todo = await _context.Todos.FindAsync(id);
        if (todo != null)
        {
            todo.IsCompleted = !todo.IsCompleted;
            await _context.SaveChangesAsync();
        }
        return RedirectToAction(nameof(Index));
    }

    // GET: Home/Create
    public IActionResult Create()
    {
        return View();
    }

    // POST: Home/Create
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Create([Bind("Title,Description,DueDate")] Todo todo)
    {
        if (ModelState.IsValid)
        {
            _context.Add(todo);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }
        return View(todo);
    }

    // GET: Home/Edit/5
    public async Task<IActionResult> Edit(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var todo = await _context.Todos.FindAsync(id);
        if (todo == null)
        {
            return NotFound();
        }
        return View(todo);
    }

    // POST: Home/Edit/5
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Edit(int id, [Bind("Id,Title,Description,DueDate,IsCompleted")] Todo todo)
    {
        if (id != todo.Id)
        {
            return NotFound();
        }

        if (ModelState.IsValid)
        {
            try
            {
                _context.Update(todo);
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateConcurrencyException)
            {
                if (!TodoExists(todo.Id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }
            return RedirectToAction(nameof(Index));
        }
        return View(todo);
    }

    // GET: Home/PastTodos
    public IActionResult PastTodos()
    {
        var previousMonthDate = DateTime.Now.AddMonths(-1);
        var pastTodos = _context.Todos
            .Where(t => t.DueDate.HasValue &&
                        t.DueDate.Value.Year == previousMonthDate.Year &&
                        t.DueDate.Value.Month == previousMonthDate.Month)
            .OrderBy(t => t.DueDate)
            .ToList();

        var groupedTodos = GroupTodosByMonthAndWeek(pastTodos);
        ViewData["Title"] = "Vergangene Todos";

        return View(groupedTodos);
    }

    private IEnumerable<GroupedTodos> GroupTodosByMonthAndWeek(IEnumerable<Todo> todos)
    {
        return todos
            .GroupBy(t => new { Year = t.DueDate.Value.Year, Month = t.DueDate.Value.Month })
            .OrderBy(g => g.Key.Year)
            .ThenBy(g => g.Key.Month)
            .Select(g => new GroupedTodos
            {
                Year = g.Key.Year,
                Month = g.Key.Month,
                Weeks = g.GroupBy(t => GetWeekOfMonth(t.DueDate.Value))
                         .OrderBy(weekGroup => weekGroup.Key)
                         .ToList()
            });
    }

    private int GetWeekOfMonth(DateTime date)
    {
        var firstDayOfMonth = new DateTime(date.Year, date.Month, 1);
        int firstWeekOfMonth = _calendar.GetWeekOfYear(firstDayOfMonth, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
        int currentWeek = _calendar.GetWeekOfYear(date, CalendarWeekRule.FirstDay, DayOfWeek.Monday);
        return currentWeek - firstWeekOfMonth + 1;
    }

    private string GetWeekDescription(int weekOfMonth)
    {
        return $"{weekOfMonth}. Woche";
    }

    private string GetMonthName(int month)
    {
        return new DateTime(1, month, 1).ToString("MMMM");
    }

    // GET: Home/Delete/5
    public async Task<IActionResult> Delete(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var todo = await _context.Todos
            .FirstOrDefaultAsync(m => m.Id == id);
        if (todo == null)
        {
            return NotFound();
        }

        return View(todo);
    }

    // POST: Home/Delete/5
    [HttpPost, ActionName("Delete")]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> DeleteConfirmed(int id)
    {
        var todo = await _context.Todos.FindAsync(id);
        _context.Todos.Remove(todo);
        await _context.SaveChangesAsync();
        return RedirectToAction(nameof(Index));
    }

    // GET: Home/Details/5
    public async Task<IActionResult> Details(int? id)
    {
        if (id == null)
        {
            return NotFound();
        }

        var todo = await _context.Todos
            .FirstOrDefaultAsync(m => m.Id == id);
        if (todo == null)
        {
            return NotFound();
        }

        return View(todo);
    }

    private bool TodoExists(int id)
    {
        return _context.Todos.Any(e => e.Id == id);
    }
}

public class GroupedTodos
{
    public int Year { get; set; }
    public int Month { get; set; }
    public List<IGrouping<int, Todo>> Weeks { get; set; }
}

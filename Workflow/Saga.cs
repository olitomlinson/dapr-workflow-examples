using Dapr.Workflow;
public class Saga<T>
{
    private T _context;
    private List<string> _log;
    private Stack<Func<T, Task>> _compensations;
    private Func<List<string>,Task> _onCompensationError;
    private Func<List<string>,Task> _onCompensationComplete;
    public Saga(T context, List<string> log)
    {
        _context = context;
        _log = log;
        _compensations = new Stack<Func<T, Task>>();
    }

    public void OnCompensationError(Func<List<string>, Task> onCompensationError)
    {
        _onCompensationError = onCompensationError;
    }

    public void OnCompensationComplete(Func<List<string>, Task> onCompensationComplete)
    {
        _onCompensationComplete = onCompensationComplete;
    }

    public void AddCompensation(Func<T, Task> compensation)
    {
        _compensations.Push(compensation);
    }

    public async Task CompensateAsync()
    {
        int i = 0;
        while (_compensations.Count > 0)
        {
            i++;
            var c = _compensations.Pop();

            try
            {
                _log.Add($"Attempting compensation {i}...");
                await c.Invoke(_context);
                _log.Add($"Compensation {i} successfull!");
            }
            catch
            {
                /* log details of all other compensations that have not yet been made if this is a show-stopper */
                await _onCompensationError(_log);
                return;
            }
        }
        await _onCompensationComplete(_log);
    }
}

public class Saga2
{
    private WorkflowContext _context;
    private List<string> _log;
    private Stack<Func<WorkflowContext, Task>> _compensations;
    private Func<List<string>,Task> _onCompensationError;
    private Func<List<string>,Task> _onCompensationComplete;
    public Saga2(WorkflowContext context, List<string> log)
    {
        _context = context;
        _log = log;
        _compensations = new Stack<Func<WorkflowContext, Task>>();
    }

    public void OnCompensationError(Func<List<string>, Task> onCompensationError)
    {
        _onCompensationError = onCompensationError;
    }

    public void OnCompensationComplete(Func<List<string>, Task> onCompensationComplete)
    {
        _onCompensationComplete = onCompensationComplete;
    }

    public async Task<T> CallActivityAsync<T,T2>(string name, T2 input, Func<WorkflowContext,T2,T, Task> compensation)
    {
        T res = default;
        try 
        { 
            res = await _context.CallActivityAsync<T>(name, input);
        }
        catch(Exception ex)
        {
            await this.CompensateAsync();
        }

        _compensations.Push(async (context) => { 
            await compensation(context, input, res); }
        );

        return res;
    }

    // public async Task CallActivityAsync(string activityname, object input, Func<WorkflowContext, Task> compensation)
    // {
    //     try 
    //     { 
    //         await _context.CallActivityAsync(activityname, input);
    //         _compensations.Push(compensation);
    //     }
    //     catch(Exception ex)
    //     {
    //         throw ex;
    //     }
    // }
    
    // public void AddCompensation(Func<WorkflowContext, Task> compensation)
    // {
    //     _compensations.Push(compensation);
    // }

    public async Task CompensateAsync()
    {
        int i = 0;
        while (_compensations.Count > 0)
        {
            i++;
            var c = _compensations.Pop();

            try
            {
                _log.Add($"Attempting compensation {i}...");
                await c.Invoke(_context);
                _log.Add($"Compensation {i} successfull!");
            }
            catch
            {
                /* log details of all other compensations that have not yet been made if this is a show-stopper */
                await _onCompensationError(_log);
                return;
            }
        }
        await _onCompensationComplete(_log);
    }
}
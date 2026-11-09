using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Owns the tower pads. Step 1: pad unlocking per loop. Placing, merging, moving and selling come in step 5.
/// </summary>
public class BuildManager : MonoBehaviour
{
    [Tooltip("All 10 pads, in unlock order (pad 1 first).")]
    [SerializeField] private List<TowerPad> pads = new List<TowerPad>();

    public IReadOnlyList<TowerPad> Pads => pads;

    private void Awake()
    {
        for (int i = 0; i < pads.Count; i++)
            pads[i].SetIndex(i);
    }

    /// <summary>Opens every pad whose unlock loop has been reached. Pads never lock again.</summary>
    public void UnlockPads(int loop)
    {
        foreach (TowerPad pad in pads)
            pad.SetUnlocked(loop >= pad.UnlockLoop);
    }
}

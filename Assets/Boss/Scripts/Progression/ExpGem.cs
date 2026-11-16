using System.Collections.Generic;
using UnityEngine;

/// Dropped by a dead monster. Flies to the character once inside the pickup radius.
public class ExpGem : MonoBehaviour
{
    private static readonly List<ExpGem> active = new List<ExpGem>();

    private PlayerCharacter target;

    public int Value { get; private set; }
    public bool IsMagnetized { get; private set; }

    public void Init(int value) => Value = value;

    private void OnEnable() => active.Add(this);
    private void OnDisable() => active.Remove(this);

    /// Removes every gem from the map (loop reset).
    public static void ClearAll()
    {
        foreach (ExpGem g in new List<ExpGem>(active))
            Destroy(g.gameObject);
    }

    public void MagnetTo(PlayerCharacter p)
    {
        IsMagnetized = true;
        target = p;
    }

    private void Update()
    {
        GameManager gm = GameManager.Instance;
        if (gm == null || gm.Phase != GamePhase.Survival) return;

        PlayerCharacter player = gm.Player;
        float dist = Vector2.Distance(transform.position, player.Position);

        if (!IsMagnetized && dist < player.PickupRadius) MagnetTo(player);
        if (!IsMagnetized) return;

        if (dist < gm.Config.gemCollectDistance)
        {
            target.CollectExp(this);
            Destroy(gameObject);
            return;
        }

        transform.position = Vector2.MoveTowards(transform.position, target.Position,
                                                 gm.Config.gemMagnetSpeed * Time.deltaTime);
    }
}

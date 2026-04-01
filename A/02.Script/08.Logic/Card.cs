using System;


public enum Suit
{
    Hearts,
    Diamonds,
    Clubs,
    Spades
}

public enum Rank
{
    Ace = 1, // A카드가 가장 낮은 랭크
    Two,
    Three,
    Four,
    Five,
    Six,
    Seven,
    Eight,
    Nine,
    Ten,
    Jack,
    Queen,
    King
}

public class Card : IComparable<Card>
{
    public Suit Suit { get; private set; }
    public Rank Rank { get; private set; }

    public Card(Suit suit, Rank rank)
    {
        Suit = suit;
        Rank = rank;
    }

    public override string ToString()
    {
        return $"{Rank} of {Suit}";
    }

    // 카드를 비교하는 메서드: Rank를 우선, Rank가 같으면 Suit로 비교
    public int CompareTo(Card other)
    {
        if (other == null) return 1;

        int rankComparison = Rank.CompareTo(other.Rank);
        if (rankComparison == 0)
        {
            return Suit.CompareTo(other.Suit);
        }
        return rankComparison;
    }
}

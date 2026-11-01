import { useTranslation } from "react-i18next";
import { BgtProgressBar } from "@/components/BgtProgressBar/BgtProgressBar";
import type { ProgressTone } from "@/components/BgtProgressBar/progressTones";
import { BgtText } from "@/components/BgtText/BgtText";
import { GameNightRsvpState, type GameNightRsvps } from "@/models";

interface Props {
	rsvps: GameNightRsvps[];
	compact?: boolean;
}

const segments: { state: GameNightRsvpState; key: string; tone: ProgressTone }[] = [
	{ state: GameNightRsvpState.Accepted, key: "rsvp.summary.coming", tone: "ok" },
	{ state: GameNightRsvpState.Maybe, key: "rsvp.summary.maybe", tone: "warn" },
	{ state: GameNightRsvpState.Pending, key: "rsvp.summary.pending", tone: "muted" },
	{ state: GameNightRsvpState.Declined, key: "rsvp.summary.declined", tone: "bad" },
];

export const RsvpProgress = (props: Props) => {
	const { rsvps, compact = false } = props;
	const { t } = useTranslation("game-nights");
	const total = rsvps.length;

	const counts = segments.map((segment) => {
		const count = rsvps.filter((rsvp) => rsvp.state === segment.state).length;
		return { ...segment, count, label: t(segment.key, { count }) };
	});

	const summary = counts
		.filter((segment) => segment.count > 0)
		.map((segment) => segment.label)
		.join(" · ");

	return (
		<div className="flex flex-col gap-1.5">
			<div className="flex flex-wrap items-center justify-between gap-x-3 gap-y-1">
				<BgtText size="2" weight="bold">
					{compact ? t("rsvp.coming-of", { coming: counts[0].count, total }) : t("rsvp.invited", { count: total })}
				</BgtText>
				{!compact && (
					<BgtText size="2" color="gray" data-testid="rsvp-summary">
						{summary}
					</BgtText>
				)}
			</div>
			<BgtProgressBar
				size="thin"
				max={total}
				segments={counts.map((segment) => ({
					key: segment.key,
					label: segment.label,
					title: segment.label,
					value: segment.count,
					tone: segment.tone,
				}))}
				formatLabel={() => summary}
			/>
		</div>
	);
};

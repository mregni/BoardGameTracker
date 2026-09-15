import { Link } from "@tanstack/react-router";
import type { ComponentType, SVGProps } from "react";
import { BgtAvatar } from "@/components/BgtAvatar/BgtAvatar";
import { BgtCard } from "@/components/BgtCard/BgtCard";
import { BgtText } from "@/components/BgtText/BgtText";
import type { LeaderboardEntry } from "@/models";

interface Props {
	title: string;
	value: string;
	entry: LeaderboardEntry | null;
	emptyText: string;
	icon: ComponentType<SVGProps<SVGSVGElement>>;
}

export const LeaderboardHighlight = ({ title, value, entry, emptyText, icon: Icon }: Props) => (
	<BgtCard className="flex flex-col gap-3">
		<div className="flex items-center gap-2">
			<Icon className="size-5 text-primary" />
			<BgtText size="2" color="gray">
				{title}
			</BgtText>
		</div>
		{entry ? (
			<Link
				to="/players/$playerId"
				params={{ playerId: entry.playerId }}
				className="flex items-center gap-3 hover:text-primary"
			>
				<BgtAvatar image={entry.image} title={entry.name} size="medium" />
				<div className="flex flex-col min-w-0">
					<span className="font-semibold truncate">{entry.name}</span>
					<BgtText size="4" color="primary" className="font-bold">
						{value}
					</BgtText>
				</div>
			</Link>
		) : (
			<BgtText size="2" color="gray">
				{emptyText}
			</BgtText>
		)}
	</BgtCard>
);

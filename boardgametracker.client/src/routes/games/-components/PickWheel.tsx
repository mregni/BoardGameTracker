import { Link, useNavigate } from "@tanstack/react-router";
import { cx } from "class-variance-authority";
import { useEffect, useId, useRef, useState } from "react";
import { useTranslation } from "react-i18next";
import BgtButton from "@/components/BgtButton/BgtButton";
import { BgtFallbackImage } from "@/components/BgtImage/BgtFallbackImage";
import { useDurationRange } from "@/hooks/useDurationRange";
import { useMediaQuery } from "@/hooks/useMediaQuery";
import { usePermissions } from "@/hooks/usePermissions";
import type { Game } from "@/models";
import { formatRange } from "@/utils/rangeUtils";

interface Props {
	games: Game[];
	winnerId?: number;
	onResult: (gameId: number) => void;
	onExclude: (gameId: number) => void;
}

const RADIUS = 96;
const HUB = 16;
const FULL_TURNS = 6;
const SPIN_MS = 4200;

const point = (angle: number, radius: number) => {
	const radians = (angle * Math.PI) / 180;
	return { x: radius * Math.sin(radians), y: -radius * Math.cos(radians) };
};

const segmentPath = (start: number, end: number) => {
	if (end - start >= 360) {
		return `M 0 ${-RADIUS} A ${RADIUS} ${RADIUS} 0 1 1 0 ${RADIUS} A ${RADIUS} ${RADIUS} 0 1 1 0 ${-RADIUS} Z`;
	}
	const from = point(start, RADIUS);
	const to = point(end, RADIUS);
	return `M 0 0 L ${from.x} ${from.y} A ${RADIUS} ${RADIUS} 0 ${end - start > 180 ? 1 : 0} 1 ${to.x} ${to.y} Z`;
};

const segmentCoverBox = (start: number, end: number) => {
	const corners = [{ x: 0, y: 0 }, point(start, RADIUS), point(end, RADIUS)];
	for (let angle = 0; angle <= 360; angle += 90) {
		if (angle > start && angle < end) corners.push(point(angle, RADIUS));
	}
	const xs = corners.map((corner) => corner.x);
	const ys = corners.map((corner) => corner.y);
	const size = Math.max(Math.max(...xs) - Math.min(...xs), Math.max(...ys) - Math.min(...ys));
	return {
		x: (Math.max(...xs) + Math.min(...xs)) / 2 - size / 2,
		y: (Math.max(...ys) + Math.min(...ys)) / 2 - size / 2,
		size,
	};
};

const restingRotation = (index: number, slice: number) => 360 - (index + 0.5) * slice;

export const PickWheel = ({ games, winnerId, onResult, onExclude }: Props) => {
	const { t } = useTranslation("games");
	const navigate = useNavigate();
	const { canWrite } = usePermissions();
	const durationRange = useDurationRange();
	const clipPrefix = useId();
	const openButtonId = useId();
	const reduceMotion = useMediaQuery("(prefers-reduced-motion: reduce)");
	const wheelRef = useRef<SVGGElement>(null);
	const slice = 360 / Math.max(games.length, 1);
	const initialIndex = games.findIndex((game) => game.id === winnerId);
	const rotationRef = useRef(initialIndex >= 0 ? restingRotation(initialIndex, slice) : 0);
	const spunRef = useRef(false);
	const [spinning, setSpinning] = useState(false);

	const winnerIndex = spinning ? -1 : games.length === 1 ? 0 : games.findIndex((game) => game.id === winnerId);
	const picked = winnerIndex >= 0 ? games[winnerIndex] : undefined;

	useEffect(() => {
		if (picked && spunRef.current) document.getElementById(openButtonId)?.focus();
	}, [picked, openButtonId]);

	const spin = () => {
		const wheel = wheelRef.current;
		if (!wheel || spinning || games.length === 0) return;
		const next = Math.floor(Math.random() * games.length);
		const from = rotationRef.current;
		const to = from - (from % 360) + FULL_TURNS * 360 + restingRotation(next, slice);
		rotationRef.current = to;
		spunRef.current = true;
		if (typeof wheel.animate !== "function") {
			wheel.style.transform = `rotate(${to}deg)`;
			onResult(games[next].id);
			return;
		}
		setSpinning(true);
		const animation = wheel.animate([{ transform: `rotate(${from}deg)` }, { transform: `rotate(${to}deg)` }], {
			duration: reduceMotion ? 0 : SPIN_MS,
			easing: "cubic-bezier(0.12, 0.8, 0.18, 1)",
			fill: "forwards",
		});
		animation.onfinish = () => {
			setSpinning(false);
			onResult(games[next].id);
		};
	};

	const overlayClass = (index: number) => {
		if (winnerIndex < 0) return "fill-black/50";
		return index === winnerIndex ? "fill-black/10" : "fill-black/75";
	};

	const chord = games.length === 1 ? RADIUS : 2 * RADIUS * 0.6 * Math.sin(((slice / 2) * Math.PI) / 180);
	const fontSize = Math.max(6.5, Math.min(9.5, chord * 0.42));
	const maxChars = Math.floor((RADIUS - HUB - 10) / (fontSize * 0.56));
	const label = (title: string) => (title.length > maxChars ? `${title.slice(0, maxChars - 1)}…` : title);

	return (
		<div className="flex flex-col items-center gap-4 w-full">
			<ul className="sr-only">
				{games.map((game) => (
					<li key={game.id}>{game.title}</li>
				))}
			</ul>
			<svg viewBox="-112 -112 224 224" className="w-full max-w-[480px] aspect-square" aria-hidden="true">
				<g ref={wheelRef} style={{ transformOrigin: "0 0", transform: `rotate(${rotationRef.current}deg)` }}>
					{games.map((game, index) => {
						const start = index * slice;
						const box = segmentCoverBox(start, start + slice);
						const clipId = `${clipPrefix}-${game.id}`;
						return (
							<g key={game.id}>
								<clipPath id={clipId}>
									<path d={segmentPath(start, start + slice)} />
								</clipPath>
								<g clipPath={`url(#${clipId})`}>
									<rect x={-RADIUS} y={-RADIUS} width={RADIUS * 2} height={RADIUS * 2} className="fill-primary/40" />
									{game.image && (
										<image
											href={game.image}
											x={box.x}
											y={box.y}
											width={box.size}
											height={box.size}
											preserveAspectRatio="xMidYMid slice"
										/>
									)}
									<rect
										x={-RADIUS}
										y={-RADIUS}
										width={RADIUS * 2}
										height={RADIUS * 2}
										className={`${overlayClass(index)} transition-[fill] duration-500`}
									/>
								</g>
							</g>
						);
					})}
					{games.length > 1 &&
						games.map((game, index) => {
							const edge = point(index * slice, RADIUS);
							return (
								<line
									key={`divider-${game.id}`}
									x1={0}
									y1={0}
									x2={edge.x}
									y2={edge.y}
									className="stroke-background"
									strokeWidth={1.5}
								/>
							);
						})}
					{games.map((game, index) => {
						const mid = index * slice + slice / 2;
						const leftHalf = games.length > 1 && mid > 180;
						const distance = games.length === 1 ? 0 : (RADIUS + HUB) / 2;
						return (
							<text
								key={`label-${game.id}`}
								transform={
									leftHalf
										? `rotate(${mid + 90}) translate(${-distance} 0)`
										: `rotate(${mid - 90}) translate(${distance} 0)`
								}
								textAnchor="middle"
								dominantBaseline="middle"
								paintOrder="stroke"
								strokeWidth={fontSize * 0.28}
								strokeLinejoin="round"
								fontSize={fontSize}
								className="fill-white stroke-black/80 font-bold"
							>
								<title>{game.title}</title>
								{label(game.title)}
							</text>
						);
					})}
					<circle r={RADIUS + 2} fill="none" className="stroke-primary" strokeWidth={6} />
					{games.length > 1 &&
						games.map((game, index) => {
							const peg = point(index * slice, RADIUS + 2);
							return <circle key={`peg-${game.id}`} cx={peg.x} cy={peg.y} r={2.2} className="fill-white" />;
						})}
					{winnerIndex >= 0 && games.length > 1 && (
						<path
							d={segmentPath(winnerIndex * slice, (winnerIndex + 1) * slice)}
							fill="none"
							className="stroke-white"
							strokeWidth={2.5}
							strokeLinejoin="round"
						/>
					)}
				</g>
				<circle r={HUB} className="fill-card stroke-primary" strokeWidth={4} />
				<path
					d="M -9 -111 L 9 -111 L 0 -90 Z"
					className="fill-white stroke-background"
					strokeWidth={1.5}
					strokeLinejoin="round"
				/>
			</svg>
			<p className="sr-only" aria-live="polite">
				{picked ? t("pick.result", { title: picked.title }) : spinning ? t("pick.spinning") : ""}
			</p>
			{picked ? (
				<div className="flex flex-col sm:flex-row items-center gap-4 w-full max-w-[480px] rounded-xl border border-primary/30 bg-primary/10 p-4">
					<div className="size-24 shrink-0 overflow-hidden rounded-lg">
						<BgtFallbackImage title={picked.title} image={picked.image} decorative />
					</div>
					<div className="flex flex-col gap-2 min-w-0 flex-1 items-center sm:items-start text-center sm:text-left">
						<span className="text-sm text-white/60">{t("pick.tonight")}</span>
						<span className="text-xl font-bold text-white">{picked.title}</span>
						<span className="text-sm text-card-value">
							{t("pick.players", { range: formatRange(picked.minPlayers, picked.maxPlayers) ?? "-" })}
							{" · "}
							{durationRange(picked.minPlayTime, picked.maxPlayTime) ?? "-"}
						</span>
						<div className="flex flex-wrap gap-2 justify-center sm:justify-start">
							<BgtButton
								id={openButtonId}
								onClick={() => navigate({ to: "/games/$gameId", params: { gameId: picked.id } })}
							>
								{t("pick.open")}
							</BgtButton>
							{canWrite && (
								<Link
									to="/sessions/new/$gameId"
									params={{ gameId: picked.id }}
									className="inline-flex items-center px-3 py-2 rounded font-bold border border-cancel/60 text-cancel hover:bg-cancel/10"
								>
									{t("pick.log")}
								</Link>
							)}
						</div>
						<div className="flex gap-4 text-sm">
							<button type="button" onClick={spin} className="text-primary-text hover:underline cursor-pointer">
								{t("pick.again")}
							</button>
							{games.length > 1 && (
								<button
									type="button"
									onClick={() => onExclude(picked.id)}
									className="text-primary-text hover:underline cursor-pointer"
								>
									{t("pick.exclude")}
								</button>
							)}
						</div>
					</div>
				</div>
			) : (
				<BgtButton
					aria-disabled={spinning || undefined}
					onClick={spin}
					className={cx("min-h-11", spinning && "opacity-60 cursor-wait")}
				>
					{spinning ? t("pick.spinning") : t("pick.spin")}
				</BgtButton>
			)}
		</div>
	);
};

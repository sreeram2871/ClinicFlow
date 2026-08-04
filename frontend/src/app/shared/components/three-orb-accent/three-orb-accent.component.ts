import { AfterViewInit, Component, ElementRef, OnDestroy, ViewChild } from '@angular/core';
import * as THREE from 'three';

@Component({
  selector: 'app-three-orb-accent',
  standalone: true,
  templateUrl: './three-orb-accent.component.html',
  styleUrl: './three-orb-accent.component.scss',
})
export class ThreeOrbAccentComponent implements AfterViewInit, OnDestroy {
  @ViewChild('canvasRef', { static: true })
  private canvasRef!: ElementRef<HTMLCanvasElement>;

  private scene: THREE.Scene | null = null;
  private camera: THREE.PerspectiveCamera | null = null;
  private renderer: THREE.WebGLRenderer | null = null;

  private knotMesh: THREE.Mesh<THREE.TorusKnotGeometry, THREE.MeshBasicMaterial> | null = null;
  private animationFrameId: number | null = null;
  private resizeHandler: (() => void) | null = null;

  ngAfterViewInit(): void {
    const canvas = this.canvasRef.nativeElement;

    this.scene = new THREE.Scene();

    this.camera = new THREE.PerspectiveCamera(50, 1, 0.1, 100);
    this.camera.position.set(2.6, 1.9, 7.2);
    this.camera.lookAt(1.2, 0.8, 0);

    this.renderer = new THREE.WebGLRenderer({
      canvas,
      antialias: true,
      alpha: true,
    });
    this.renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));

    const geometry = new THREE.TorusKnotGeometry(1.3, 0.24, 180, 24);
    const material = new THREE.MeshBasicMaterial({
      color: 0x0f6e56,
      wireframe: true,
    });

    this.knotMesh = new THREE.Mesh(geometry, material);
    this.knotMesh.position.set(1.4, 1.0, 0);
    this.scene.add(this.knotMesh);

    this.resizeHandler = () => this.updateRendererSize();
    window.addEventListener('resize', this.resizeHandler);

    this.updateRendererSize();
    this.animate();
  }

  ngOnDestroy(): void {
    if (this.animationFrameId !== null) {
      cancelAnimationFrame(this.animationFrameId);
      this.animationFrameId = null;
    }

    if (this.resizeHandler) {
      window.removeEventListener('resize', this.resizeHandler);
      this.resizeHandler = null;
    }

    if (this.knotMesh) {
      this.knotMesh.geometry.dispose();
      this.knotMesh.material.dispose();
      this.scene?.remove(this.knotMesh);
      this.knotMesh = null;
    }

    if (this.renderer) {
      this.renderer.dispose();
      this.renderer.forceContextLoss();
      this.renderer = null;
    }

    this.camera = null;
    this.scene = null;
  }

  private updateRendererSize(): void {
    const canvas = this.canvasRef.nativeElement;
    const parent = canvas.parentElement;

    if (!this.renderer || !this.camera || !parent) {
      return;
    }

    const { width, height } = parent.getBoundingClientRect();
    if (width === 0 || height === 0) {
      return;
    }

    this.renderer.setSize(width, height, false);
    this.camera.aspect = width / height;
    this.camera.updateProjectionMatrix();
  }

  private animate = (): void => {
    if (!this.renderer || !this.scene || !this.camera || !this.knotMesh) {
      return;
    }

    this.knotMesh.rotation.x += 0.003;
    this.knotMesh.rotation.y += 0.0024;

    this.renderer.render(this.scene, this.camera);
    this.animationFrameId = requestAnimationFrame(this.animate);
  };
}

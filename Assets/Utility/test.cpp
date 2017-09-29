#define HINIBBLE(b) (((b) >> 4) & 0x0F)
#define LONIBBLE(b) ((b)&0x0F)

enum
{
    PRIMARY = 0,
    SECONDARY = 1
};

struct vertex
{
    Vector3d pos[2]; // The primary and secondary positions of the vertex.
    Vector3d normal; // The surface normal at the vertex.
    u8 near;         // The proximity of the vertex to the six faces of the block.
};

struct VolumeData
{
    VolumeData(const Vector3i &dim)
        : size(dim)
    {
        offset << 0, 0, 0;
        samples.resize(dim.x() * dim.y() * dim.z(), 0);
    }

    VolumeData(const Vector3i &dim, const Vector3i &origin)
        : size(dim), offset(origin)
    {

        samples.resize(dim.x() * dim.y() * dim.z(), 0);
    }

    inline i8 &operator[](const Vector3i &p)
    {
        const Vector3i v = p - offset;
        return samples[v.x() + v.y() * size.x() + v.z() * (size.x() * size.y())];
    }

    inline const i8 &operator[](const Vector3i &p) const
    {
        const Vector3i v = p - offset;
        return samples[v.x() + v.y() * size.x() + v.z() * (size.x() * size.y())];
    }

    const char *buf() const
    {
        return &samples[0];
    }

    const std::size_t bufsize() const
    {
        return samples.size();
    }

  private:
    std::vector samples;
    Vector3i size;
    Vector3i offset;
};

const int BLOCK_WIDTH = 16;

struct regular_cell_cache
{

    struct cell
    {
        u8 caseIndex;
        int verts[4];
    };

    cell &get(int x, int y, int z)
    {
        return cache[z & 1][y * BLOCK_WIDTH + x];
    }

    cell &get(const Vector3i &p)
    {
        return get(p.x(), p.y(), p.z());
    }

  private:
    cell cache[2][BLOCK_WIDTH * BLOCK_WIDTH];
};

struct transition_cell_cache
{

    struct cell
    {
        int verts[10]; // The ten vertices that can be reused by other cells.
    };

    cell &get(int x, int y)
    {
        return cache[y & 1][x];
    }

  private:
    cell cache[2][BLOCK_WIDTH];
};

inline int sign(i8 x)
{
    return (x >> 7) & 1;
}

inline const Vector3d interp(Vector3i v0, Vector3i v1, // Position of vertices
                             Vector3i p0, Vector3i p1, // Position of sample points
                             const VolumeData &samples, u8 lodIndex = 0)
{
    i8 s0 = samples[p0];
    i8 s1 = samples[p1];

    i32 t = (s1 << 8) / (s1 - s0);
    i32 u = 0x0100 - t;
    const double s = 1.0 / 256.0;

    if ((t & 0x00ff) == 0)
    {
        // The generated vertex lies at one of the corners so there
        // is no need to subdivide the interval.
        if (t == 0)
        {
            return v1.cast();
        }
        return v0.cast();
    }
    else
    {
        for (u8 i = 0; i < lodIndex; ++i)
        {
            const Vector3i vm = (v0 + v1) / 2;
            const Vector3i pm = (p0 + p1) / 2;

            const u8 sm = samples[pm];

            // Determine which of the sub-intervals that contain
            // the intersection with the isosurface.
            if (sign(s0) != sign(sm))
            {
                v1 = vm;
                p1 = pm;
                s1 = sm;
            }
            else
            {
                v0 = vm;
                p0 = pm;
                s0 = sm;
            }
        }
        t = (s1 << 8) / (s1 - s0);
        u = 0x0100 - t;

        return (v0.cast() * t) * s + (v1.cast() * u) * s;
    }
}

inline const Vector3d interp(Vector3d v0, Vector3d v1, // Position of vertices
                             Vector3i p0, Vector3i p1, // Position of sample points
                             const VolumeData &samples, u8 lodIndex = 0)
{
    i8 s0 = samples[p0];
    i8 s1 = samples[p1];

    i32 t = (s1 << 8) / (s1 - s0);
    i32 u = 0x0100 - t;
    const double s = 1.0 / 256.0;

    if ((t & 0x00ff) == 0)
    {
        // The generated vertex lies at one of the corners so there
        // is no need to subdivide the interval.
        if (t == 0)
        {
            return v1.cast();
        }
        return v0.cast();
    }
    else
    {
        for (u8 i = 0; i < lodIndex; ++i)
        {
            const Vector3d vm = (v0 + v1) / 2;
            const Vector3i pm = (p0 + p1) / 2;

            const u8 sm = samples[pm];

            // Determine which of the sub-intervals that contain
            // the intersection with the isosurface.
            if (sign(s0) != sign(sm))
            {
                v1 = vm;
                p1 = pm;
                s1 = sm;
            }
            else
            {
                v0 = vm;
                p0 = pm;
                s0 = sm;
            }
        }
        t = (s1 << 8) / (s1 - s0);
        u = 0x0100 - t;

        return v0 * t * s + v1 * u * s;
    }
}

inline Vector3d computeDelta(const Vector3d &v, int k, int s)
{
    const double p2k = pow(2.0, k);
    const double wk = pow(2.0, k - 2.0);
    Vector3d delta(0, 0, 0);

    if (k < 1)
    {
        return delta;
    }

    for (int i = 0; i < 3; ++i)
    {
        const double x = v[i];

        if (x < p2k)
        {
            // The vertex is inside the minimum cell.
            delta[i] = (1.0 - pow(2.0, -k) * x) * wk;
        }
        else if (x > (p2k * (s - 1)))
        {
            // The vertex is inside the maximum cell.
            delta[i] = ((p2k * s) - 1.0 - x) * wk;
        }
    }
    return delta;
}

inline Vector3d projectNormal(Vector3d N, const Vector3d &delta)
{
    Eigen::Matrix mat;
    mat << 1.0 - N.x() * N.x(), -N.x() * N.y(), -N.x() * N.z(),
        -N.x() * N.y(), 1.0 - N.y() * N.y(), -N.y() * N.z(),
        -N.x() * N.z(), -N.y() * N.z(), 1.0 - N.z() * N.z();
    return mat * delta;
}

inline Vector3i prevOffset(u8 dir)
{
    return Vector3i(-(dir & 1),
                    -((dir >> 1) & 1),
                    -((dir >> 2) & 1));
}

inline int polygonizeRegularCell(
    const Vector3i &min,
    const Vector3d &offset, // The minimum corner of the cell in object space.
    const Vector3i xyz,
    const VolumeData &samples,
    u8 lodIndex,
    double cellSize,
    std::vector &verts,
    std::vector &indices,
    regular_cell_cache *cache)
{
    const int W = 19;
    const i32 lodScale = 1 << lodIndex;
    const i32 last = 15 * lodScale;
    const u8 directionMask = (xyz.x() > 0 ? 1 : 0) | ((xyz.y() > 0 ? 1 : 0) << 1) | ((xyz.z() > 0 ? 1 : 0) << 2);

    u8 near = 0;

    // Compute which of the six faces of the block that the vertex
    // is near. (near is defined as being in boundary cell.)
    for (int i = 0; i < 3; ++i)
    {
        if (min[i] == 0)
        {
            near |= (1 << (i * 2 + 0)); /* Vertex close to negative face. */
        }
        if (min[i] == last)
        {
            near |= (1 << (i * 2 + 1)); /* Vertex close to positive face. */
        }
    }

    const Vector3i cornerPositions[] =
        {
            min + Vector3i(0, 0, 0) * lodScale,
            min + Vector3i(1, 0, 0) * lodScale,
            min + Vector3i(0, 1, 0) * lodScale,
            min + Vector3i(1, 1, 0) * lodScale,

            min + Vector3i(0, 0, 1) * lodScale,
            min + Vector3i(1, 0, 1) * lodScale,
            min + Vector3i(0, 1, 1) * lodScale,
            min + Vector3i(1, 1, 1) * lodScale};

    const Vector3i dif = cornerPositions[7] - cornerPositions[0];

    // Retrieve sample values for all the corners.
    const i8 cornerSamples[8] =
        {
            samples[cornerPositions[0]],
            samples[cornerPositions[1]],
            samples[cornerPositions[2]],
            samples[cornerPositions[3]],
            samples[cornerPositions[4]],
            samples[cornerPositions[5]],
            samples[cornerPositions[6]],
            samples[cornerPositions[7]],
        };

    Eigen::Vector3d cornerNormals[8];

    /* Determine the index into the edge table which
tells us which vertices are inside of the surface */
    const u32 caseCode = ((cornerSamples[0] >> 7) & 0x01) | ((cornerSamples[1] >> 6) & 0x02) | ((cornerSamples[2] >> 5) & 0x04) | ((cornerSamples[3] >> 4) & 0x08) | ((cornerSamples[4] >> 3) & 0x10) | ((cornerSamples[5] >> 2) & 0x20) | ((cornerSamples[6] >> 1) & 0x40) | (cornerSamples[7] & 0x80);

    cache->get(xyz).caseIndex = caseCode;

    if ((caseCode ^ ((cornerSamples[7] >> 7) & 0xff)) == 0)
        return 0;

    // Compute the normals at the cell corners using central difference.
    for (int i = 0; i < 8; ++i)
    {
        const Vector3i p = cornerPositions[i];
        const double nx = (samples[p + Vector3i::UnitX()] - samples[p - Vector3i::UnitX()]) * 0.5;
        const double ny = (samples[p + Vector3i::UnitY()] - samples[p - Vector3i::UnitY()]) * 0.5;
        const double nz = (samples[p + Vector3i::UnitZ()] - samples[p - Vector3i::UnitZ()]) * 0.5;

        cornerNormals[i] = Eigen::Vector3d(nx, ny, nz).normalized();
    }

    const char c = regularCellClass[caseCode];
    const RegularCellData &data = regularCellData[c];

    const u8 nt = (u8)data.GetTriangleCount();
    const u8 nv = (u8)data.GetVertexCount();

    int localVertexMapping[12];

    vertex vert;
    vert.near = near;

    // Generate all the vertex positions by interpolating along
    // each of the edges that intersect the isosurface.
    for (u8 i = 0; i < nv; ++i)
    {
        const u16 edgeCode = regularVertexData[caseCode][i];
        // The low byte of each 16-bit code contains the corner
        // indexes of the edge's endpoints in one nibble each.
        const u8 v0 = HINIBBLE(edgeCode & 0xff);
        const u8 v1 = LONIBBLE(edgeCode & 0xff);

        const Vector3i p0 = cornerPositions[v0];
        const Vector3i p1 = cornerPositions[v1];
        const Vector3d n0 = cornerNormals[v0];
        const Vector3d n1 = cornerNormals[v1];

        const i32 d0 = samples[p0];
        const i32 d1 = samples[p1];

        assert(v0 < v1);

        const i32 t = (d1 << 8) / (d1 - d0);
        const i32 u = 0x0100 - t;
        const double s = 1.0 / 256.0;

        const double t0 = t * s, t1 = u * s;

        if ((t & 0x00ff) != 0)
        {
            // Vertex lies in the interior of the edge.
            const u8 dir = HINIBBLE(edgeCode >> 8); // The direction to the previous cell.
            const u8 idx = LONIBBLE(edgeCode >> 8); // The vertex to generate or reuse for this edge (0-3)
            const bool present = (dir & directionMask) == dir;

            if (present)
            {
                const regular_cell_cache::cell &prev = cache->get(xyz + prevOffset(dir));
                // I don't think this can happen for non-corner vertices.
                if (prev.caseIndex == 0 || prev.caseIndex == 255)
                {
                    localVertexMapping[i] = -1;
                }
                else
                {
                    localVertexMapping[i] = prev.verts[idx];
                }
            }

            if (!present || localVertexMapping[i] < 0)
            {
                localVertexMapping[i] = verts.size();
                // Compute the intersection between the edge and the isosurface
                // using the highest resolution sample data to get correct position.
                const Vector3d pi = interp(p0.cast().eval(), p1.cast().eval(), p0, p1, samples, lodIndex);

                vert.pos[PRIMARY] = offset + pi;
                vert.normal = n0 * t0 + n1 * t1;

                if (near)
                {
                    const Vector3d delta = computeDelta(pi, lodIndex, 16);
                    vert.pos[SECONDARY] = vert.pos[PRIMARY] + projectNormal(vert.normal, delta);
                }
                else
                {
                    // The vertex is not in a boundary cell, so the
                    // secondary position will never be used.
                    vert.pos[SECONDARY] = Vector3d(1000, 1000, 1000); //vert.pos[PRIMARY];
                }
                verts.push_back(vert);

                if ((dir & 8) != 0)
                {
                    // Store the generated vertex so that other cells can reuse it.
                    cache->get(xyz).verts[idx] = localVertexMapping[i];
                }
            }
        }
        else if (t == 0 && v1 == 7)
        {
            // This cell owns the vertex, so it should be created.
            localVertexMapping[i] = verts.size();

            const Eigen::Vector3d pi = p0.cast() * t0 + p1.cast() * t1;

            vert.pos[PRIMARY] = offset + pi;
            vert.normal = n0 * t0 + n1 * t1;

            if (near)
            {
                const Vector3d delta = computeDelta(pi, lodIndex, 16);
                vert.pos[SECONDARY] = vert.pos[PRIMARY] + projectNormal(vert.normal, delta);
            }
            else
            {
                // The vertex is not in a boundary cell, so the secondary
                // position will never be used.
                vert.pos[SECONDARY] = Vector3d(1000, 1000, 1000);
            }
            verts.push_back(vert);
            cache->get(xyz).verts[0] = localVertexMapping[i];
        }
        else
        {
            // A 3-bit direction code leading to the proper cell can easily be obtained by
            // inverting the 3-bit corner index (bitwise, by exclusive ORing with the number 7).
            // The corner index depends on the value of t, t = 0 means that we're at the higher
            // numbered endpoint.
            const u8 dir = t == 0 ? (v1 ^ 7) : (v0 ^ 7);
            const bool present = (dir & directionMask) == dir;

            if (present)
            {
                const regular_cell_cache::cell &prev = cache->get(xyz + prevOffset(dir));
                // The previous cell might not have any geometry, and we
                // might therefore have to create a new vertex anyway.
                if (prev.caseIndex == 0 || prev.caseIndex == 255)
                {
                    localVertexMapping[i] = -1;
                }
                else
                {
                    localVertexMapping[i] = prev.verts[0];
                }
            }

            if (!present || (localVertexMapping[i] < 0))
            {
                localVertexMapping[i] = verts.size();

                const Eigen::Vector3d pi = p0.cast() * t0 + p1.cast() * t1;

                vert.pos[PRIMARY] = offset + pi;
                vert.normal = n0 * t0 + n1 * t1;

                if (near)
                {
                    const Vector3d delta = computeDelta(pi, lodIndex, 16);
                    vert.pos[SECONDARY] = vert.pos[PRIMARY] + projectNormal(vert.normal, delta);
                }
                else
                {
                    // The vertex is not in a boundary cell, so the secondary
                    // position will never be used.
                    vert.pos[SECONDARY] = Vector3d(1000, 1000, 1000); //vert.pos[PRIMARY];
                }
                verts.push_back(vert);
            }
        }
    }

    for (long t = 0; t < nt; ++t)
    {
        // TODO: Add check for zero-area triangles here.
        for (int i = 0; i < 3; ++i)
        {
            indices.push_back(localVertexMapping[data.vertexIndex[t * 3 + i]]);
        }
    }

    return nt;
}

inline int polygonizeTransitionCell(
    const Vector3d &offset, // Offset of the cell in world space.
    const Vector3i &origin, // Origin in sample space.
    const Vector3i &localX,
    const Vector3i &localY,
    const Vector3i &localZ,
    int x, int y,   // The x and y position of the cell within the block face.
    float cellSize, // The width of a cell in world scale.
    u8 lodIndex,
    u8 axis,          // The index of the axis corresponding to the z-axis.
    u8 directionMask, // Used to determine which previous cells that are available.
    const VolumeData &samples,
    std::vector &verts,
    std::vector &indices,
    transition_cell_cache *cache)
{
    const int lodStep = 1 << lodIndex;
    const int sampleStep = 1 << (lodIndex - 1);
    const i32 lodScale = 1 << lodIndex;
    const i32 last = 16 * lodScale;

    u8 near = 0;
    // Compute which of the six faces of the block that the vertex
    // is near. (near is defined as being in boundary cell.)
    for (int i = 0; i < 3; ++i)
    {
        if (origin[i] == 0)
        {
            near |= (1 << (i * 2 + 0)); /* Vertex close to negative face. */
        }
        if (origin[i] == last)
        {
            near |= (1 << (i * 2 + 1)); /* Vertex close to positive face. */
        }
    }

    static const Vector3i coords[] = {
        Vector3i(0, 0, 0), Vector3i(1, 0, 0), Vector3i(2, 0, 0), // High-res lower row
        Vector3i(0, 1, 0), Vector3i(1, 1, 0), Vector3i(2, 1, 0), // High-res middle row
        Vector3i(0, 2, 0), Vector3i(1, 2, 0), Vector3i(2, 2, 0), // High-res upper row

        Vector3i(0, 0, 2), Vector3i(2, 0, 2), // Low-res lower row
        Vector3i(0, 2, 2), Vector3i(2, 2, 2)  // Low-res upper row
    };

    Eigen::Matrix basis;

    basis.col(0) = localX * sampleStep;
    basis.col(1) = localY * sampleStep;
    basis.col(2) = localZ * sampleStep;

    // The positions of each voxel corner in local block space.
    const Vector3i pos[] = {
        origin + basis * coords[0x00], origin + basis * coords[0x01], origin + basis * coords[0x02],
        origin + basis * coords[0x03], origin + basis * coords[0x04], origin + basis * coords[0x05],
        origin + basis * coords[0x06], origin + basis * coords[0x07], origin + basis * coords[0x08],
        origin + basis * coords[0x09], origin + basis * coords[0x0A],
        origin + basis * coords[0x0B], origin + basis * coords[0x0C],
    };

    Vector3d normals[13];

    // Compute the normals at the cell corners using central difference.
    for (int i = 0; i < 9; ++i)
    {
        const Vector3i p = pos[i];
        const double nx = (samples[p + Vector3i::UnitX()] - samples[p - Vector3i::UnitX()]) * 0.5;
        const double ny = (samples[p + Vector3i::UnitY()] - samples[p - Vector3i::UnitY()]) * 0.5;
        const double nz = (samples[p + Vector3i::UnitZ()] - samples[p - Vector3i::UnitZ()]) * 0.5;

        normals[i] = Eigen::Vector3d(nx, ny, nz).normalized();
    }

    normals[0x9] = normals[0];
    normals[0xA] = normals[2];
    normals[0xB] = normals[6];
    normals[0xC] = normals[8];

    const Vector3i samplePos[] = {
        pos[0], pos[1], pos[2],
        pos[3], pos[4], pos[5],
        pos[6], pos[7], pos[8],
        pos[0], pos[2],
        pos[6], pos[8]};

    const u32 caseCode = sign(samples[pos[0]]) * 0x001 |
                         sign(samples[pos[1]]) * 0x002 |
                         sign(samples[pos[2]]) * 0x004 |
                         sign(samples[pos[5]]) * 0x008 |
                         sign(samples[pos[8]]) * 0x010 |
                         sign(samples[pos[7]]) * 0x020 |
                         sign(samples[pos[6]]) * 0x040 |
                         sign(samples[pos[3]]) * 0x080 |
                         sign(samples[pos[4]]) * 0x100;

    if (caseCode == 0 || caseCode == 511)
        return 0;

    cache->get(x, y).caseIndex = caseCode;

    const u8 classIndex = transitionCellClass[caseCode]; // Equivalence class index.
    const TransitionCellData &data = transitionCellData[classIndex & 0x7F];

    const bool inverse = (classIndex & 128) != 0;

    int localVertexMapping[12];

    const long nv = data.GetVertexCount();
    const long nt = data.GetTriangleCount();

    assert(nv <= 12);
    vertex vert;

    for (long i = 0; i < nv; ++i)
    {
        const u16 edgeCode = transitionVertexData[caseCode][i];
        // The low byte of each 16-bit code contains the corner
        // indexes of the edge's endpoints in one nibble each.
        const u8 v0 = HINIBBLE(edgeCode);
        const u8 v1 = LONIBBLE(edgeCode);
        const bool lowside = (v0 > 8) && (v1 > 8);

        const i32 d0 = samples[samplePos[v0]];
        const i32 d1 = samples[samplePos[v1]];

        const i32 t = (d1 << 8) / (d1 - d0);
        const i32 u = 0x0100 - t;
        const double s = 1.0 / 256.0;
        const double t0 = t * s, t1 = u * s;

        const Vector3d n0 = normals[v0];
        const Vector3d n1 = normals[v1];

        vert.near = near;
        vert.normal = n0 * t0 + n1 * t1;

        if ((t & 0x00ff) != 0)
        {
            // Use the reuse information in transitionVertexData
            const u8 dir = HINIBBLE(edgeCode >> 8);
            const u8 idx = LONIBBLE(edgeCode >> 8);
            const bool present = (dir & directionMask) == dir;

            if (present)
            {
                // The previous cell is available. Retrieve the cached cell
                // from which to retrieve the reused vertex index from.
                const transition_cell_cache::cell &prev = cache->get(x - (dir & 1), y - ((dir >> 1) & 1));

                if (prev.caseIndex == 0 || prev.caseIndex == 511)
                {
                    // Previous cell does not contain any geometry.
                    localVertexMapping[i] = -1;
                }
                else
                {
                    // Reuse the vertex index from the previous cell.
                    localVertexMapping[i] = prev.verts[idx];
                }
            }

            if (!present || localVertexMapping[i] < 0)
            {
                // A vertex has to be created.
                const Vector3d p0 = pos[v0].cast();
                const Vector3d p1 = pos[v1].cast();

                Vector3d pi = interp(p0, p1, samplePos[v0], samplePos[v1], samples, lowside ? lodIndex : lodIndex - 1);

                if (lowside)
                {
                    // Necessary to translate the intersection point to the
                    // high-res side so that it is transformed the same way
                    // as the vertices in the regular cell.
                    pi[axis] = (double)origin[axis];

                    const Vector3d delta = computeDelta(pi, lodIndex, 16);
                    const Vector3d proj = projectNormal(vert.normal, delta);

                    vert.pos[PRIMARY] = Vector3d(1000, 1000, 1000);
                    vert.pos[SECONDARY] = offset + pi + proj;
                }
                else
                {
                    vert.near = 0; // Vertices on high-res side are never moved.
                    vert.pos[PRIMARY] = offset + pi;
                    vert.pos[SECONDARY] = Vector3d(1000, 1000, 1000);
                }

                localVertexMapping[i] = verts.size();
                verts.push_back(vert);

                if ((dir & 8) != 0)
                {
                    // The vertex can be reused.
                    cache->get(x, y).verts[idx] = localVertexMapping[i];
                }
            }
        }
        else
        {
            // Try to reuse corner vertex from a preceding cell.
            // Use the reuse information in transitionCornerData.
            const u8 v = t == 0 ? v1 : v0;
            const u8 cornerData = transitionCornerData[v];

            const u8 dir = HINIBBLE(cornerData); // High nibble contains direction code.
            const u8 idx = LONIBBLE(cornerData); // Low nibble contains storage slot for vertex.
            const bool present = (dir & directionMask) == dir;

            if (present)
            {
                // The previous cell is available. Retrieve the cached cell
                // from which to retrieve the reused vertex index from.
                const transition_cell_cache::cell &prev = cache->get(x - (dir & 1), y - ((dir >> 1) & 1));

                if (prev.caseIndex == 0 || prev.caseIndex == 511)
                {
                    // Previous cell does not contain any geometry.
                    localVertexMapping[i] = -1;
                }
                else
                {
                    // Reuse the vertex index from the previous cell.
                    localVertexMapping[i] = prev.verts[idx];
                }
            }

            if (!present || localVertexMapping[i] < 0)
            {
                // A vertex has to be created.
                Vector3d pi = pos[v].cast();

                if (v > 8)
                {
                    // On low-resolution side.
                    // Necessary to translate the intersection point to the
                    // high-res side so that it is transformed the same way
                    // as the vertices in the regular cell.
                    pi[axis] = (double)origin[axis];

                    const Vector3d delta = computeDelta(pi, lodIndex, 16);
                    const Vector3d proj = projectNormal(vert.normal, delta);

                    vert.pos[PRIMARY] = Vector3d(1000, 1000, 1000);
                    vert.pos[SECONDARY] = offset + pi + proj;
                }
                else
                {
                    // On high-resolution side.
                    vert.near = 0; // Vertices on high-res side are never moved.
                    vert.pos[PRIMARY] = offset + pi;
                    vert.pos[SECONDARY] = Vector3d(1000, 1000, 1000);
                }
                localVertexMapping[i] = verts.size();
                cache->get(x, y).verts[idx] = localVertexMapping[i];
                verts.push_back(vert);
            }
        }
    }

    for (long t = 0; t < nt; ++t)
    {
        const u8 *ptr = &data.vertexIndex[t * 3];

        if (inverse)
        {
            indices.push_back(localVertexMapping[ptr[2]]);
            indices.push_back(localVertexMapping[ptr[1]]);
            indices.push_back(localVertexMapping[ptr[0]]);
        }
        else
        {
            indices.push_back(localVertexMapping[ptr[0]]);
            indices.push_back(localVertexMapping[ptr[1]]);
            indices.push_back(localVertexMapping[ptr[2]]);
        }
    }

    return nt;
}

Usage example :
    [code] void
    generateNegativeXTransitionCells(
        const VolumeData &samples,
        const Vector3d &offset,
        const Vector3i &min,
        float cellSize,
        u8 lodIndex,
        std::vector &verts,
        std::vector &indices) {
        const int spacing = 1 << lodIndex; // Spacing between low-res corners.

        const Vector3i origin = min + Vector3i(0, 0, 16);
        const Vector3i xAxis = Vector3i(0, 0, -1);
        const Vector3i yAxis = Vector3i(0, 1, 0);
        const Vector3i zAxis = Vector3i(1, 0, 0);

        transition_cell_cache cache;

        for (int y = 0; y < 16; ++y)
        {
            for (int x = 0; x < 16; ++x)
            {
                const Vector3i p = (origin + xAxis * x + yAxis * y) * spacing;
                const u8 directionMask = (x > 0 ? 1 : 0) | ((y > 0 ? 1 : 0) << 1);

                polygonizeTransitionCell(offset, p, xAxis, yAxis, zAxis, x, y, cellSize, lodIndex, 0, directionMask, samples, verts, indices, &cache);
            }
        }
    }